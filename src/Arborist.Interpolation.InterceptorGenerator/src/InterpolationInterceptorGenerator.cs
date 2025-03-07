using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text.RegularExpressions;

namespace Arborist.Interpolation.InterceptorGenerator;

[Generator]
public class InterpolationInterceptorGenerator : IIncrementalGenerator {
    // InterceptorsNamespaces is not supported by the current LTS SDK release (the 8.0.1XX line),
    // so for now we ask for InterceptorsPreviewNamespaces
    public const string INTERCEPTORSNAMESPACES_BUILD_PROP = "InterceptorsPreviewNamespaces";
    public const string INTERCEPTOR_NAMESPACE = "Arborist.Interpolation.Interceptors";

    public Action<InterpolationAnalysisResult>? AnalysisResultHandler { get; set; }

    public static class StepNames {
        public const string Analyses = nameof(Analyses);
        public const string AnalyzerOptions = nameof(AnalyzerOptions);
        public const string GroupedAnalyses = nameof(GroupedAnalyses);
    }

    // The design for this source generator is partially based on the ASP.net Core RequestDelegateGenerator
    // https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http.Extensions/gen/RequestDelegateGenerator.cs
    // We do the full analysis of each interpolation in the syntax provider transform. This is probably
    // the most correct approach to maintaining "incrementality" in that even an approach based on the
    // equality of the syntax tree of the invocation will not protect against changes to external sources.
    //
    // Our analysis is fairly intensive, and our generator does not need to be incremental as it does not
    // generate user-callable code (honestly incrementality is a pain in the butt as it's designed around the
    // IDE experience); however it seems inadvisable to explore more aggressive caching solutions as I have
    // concerns around potential impacts to e.g. hot reload.
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var analysisResults = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: SyntaxProviderPredicate,
            transform: SyntaxProviderTransform
        )
        .Where(static tup => tup is not null)
        .Select(static (tup, _) => tup!.Value)
        .WithTrackingName(StepNames.Analyses);

        // It's interesting that there does not appear to be a way to access AnalyzerConfigOptionsProvider
        // from the GeneratorSyntaxContext, which makes it very difficult to apply options to your syntax
        // analysis. Fortunately in our case we don't have any options affecting the analysis.
        var interceptorsEnabledProvider = context.AnalyzerConfigOptionsProvider
        .Select(static (analyzerOptions, _) => GetInterceptorsEnabled(analyzerOptions))
        .WithTrackingName(StepNames.AnalyzerOptions);

        var analysesAndEnabled = analysisResults.Combine(interceptorsEnabledProvider);

        context.RegisterSourceOutput(analysesAndEnabled, (spc, inputs) => {
            var ((diagnostics, analysis), interceptorsEnabled) = inputs;

            // Report the analysis result to the configured handler
            if(analysis is not null)
                AnalysisResultHandler?.Invoke(analysis);

            // If interceptors are disabled, emit an informational diagnostic for each successful analysis
            // prompting the user to enable them.
            if(!interceptorsEnabled && analysis?.IsSupported is true)
                spc.ReportDiagnostic(Diagnostic.Create(
                    descriptor: InterpolationDiagnostics.SetInterceptorsNamespaces(
                        severity: analysis.InterceptionRequired ? DiagnosticSeverity.Error : null
                    ),
                    location: analysis.InvocationLocation
                ));

            foreach(var diagnostic in diagnostics.CollectedDiagnostics.Distinct())
                spc.ReportDiagnostic(diagnostic);
        });

        var groupedAnalyses = analysisResults
        .Where(static tup => tup.Item2 is not null && tup.Item2.IsSupported)
        .Select(static (tup, _) => tup.Item2!)
        .Collect()
        .SelectMany(InterpolationAnalysisGroup.CreateGroups)
        .WithTrackingName(StepNames.GroupedAnalyses);

        var analysisGroupsAndEnabled = groupedAnalyses.Combine(interceptorsEnabledProvider);

        context.RegisterSourceOutput(analysisGroupsAndEnabled, (spc, inputs) => {
            var (analysisGroup, interceptorsEnabled) = inputs;
            if(interceptorsEnabled)
                spc.AddSource(
                    hintName: analysisGroup.GeneratedSourceName,
                    source: RenderInterceptorGroup(analysisGroup, spc.CancellationToken)
                );
        });

        context.RegisterSourceOutput(interceptorsEnabledProvider, static (spc, interceptorsEnabled) => {
            // If interceptors are not enabled (and we are therefore not emitting any), emit a generated
            // artifact containing a helpful message (as the info diagnostics are not particularly visible)
            if(!interceptorsEnabled)
                spc.AddSource(
                    hintName: "InterceptorsDisabled.g.cs",
                    source: string.Join("\n", [
                        $"// Add {INTERCEPTOR_NAMESPACE} to the {INTERCEPTORSNAMESPACES_BUILD_PROP} build property",
                        $"// to enable compile-time expression interpolation.",
                        $""
                    ])
                );
        });
    }

    private static bool SyntaxProviderPredicate(SyntaxNode node, CancellationToken cancellationToken) =>
        // Run the transform on every invocation of a method whose name starts with "Interpolate".
        // This restriction dramatically reduces the number of input nodes to the source generator.
        node is InvocationExpressionSyntax invocation
        && TryGetInvocationMethodIdentifier(invocation, out var identifier)
        && identifier.ValueText.StartsWith("Interpolate");

    private static (InterpolationDiagnosticsCollector, InterpolationAnalysisResult?)? SyntaxProviderTransform(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken
    ) {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if(context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
            return null;
        if(!methodSymbol.GetAttributes().Any(IsExpressionInterpolatorAttribute))
            return null;

        return InterpolationAnalyzer.Analyze(context.SemanticModel, invocation, cancellationToken);
    }

    internal static bool TryGetInvocationMethodIdentifier(InvocationExpressionSyntax invocation, out SyntaxToken identifier) {
        switch(invocation.Expression) {
            case MemberAccessExpressionSyntax mae:
                identifier = mae.Name.Identifier;
                return true;

            case SimpleNameSyntax sns:
                identifier = sns.Identifier;
                return true;

            default:
                identifier = default;
                return false;
        }
    }

    private static bool IsExpressionInterpolatorAttribute(AttributeData a) =>
        a.AttributeClass is {
            Name: "InterceptedExpressionInterpolatorAttribute",
            ContainingNamespace: {
                Name: "Internal",
                ContainingNamespace: {
                    Name: "Interpolation",
                    ContainingNamespace: {
                        Name: "Arborist",
                        ContainingNamespace: { IsGlobalNamespace: true }
                    }
                }
            }
        };

    private static bool GetInterceptorsEnabled(AnalyzerConfigOptionsProvider analyzerOptions) {
        if(!analyzerOptions.GlobalOptions.TryGetValue("build_property._ArboristInterceptorsNamespaces", out var propertyValue))
            return false;
        if(Regex.IsMatch(propertyValue, $@"(^|\s+){Regex.Escape(INTERCEPTOR_NAMESPACE)}(\s+|$)"))
            return true;

        return false;
    }

    private static string RenderInterceptorGroup(
        InterpolationAnalysisGroup analysisGroup,
        CancellationToken cancellationToken
    ) {
        using var writer = PooledStringWriter.Rent();

        writer.WriteLine("#nullable enable");
        writer.WriteLine();
        writer.WriteLine("namespace System.Runtime.CompilerServices {");
        writer.WriteLine("    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]");
        writer.WriteLine("    file sealed class InterceptsLocationAttribute : global::System.Attribute {");
        writer.WriteLine("        public InterceptsLocationAttribute(string filePath, int line, int column) { }");
        writer.WriteLine("    }");
        writer.WriteLine("}");
        writer.WriteLine();
        writer.WriteLine($"namespace {INTERCEPTOR_NAMESPACE} {{");

        writer.WriteLine($"    file static class {analysisGroup.ClassName} {{");

        foreach(var analysis in analysisGroup.Analyses) {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteLine();
            RenderInterceptor(writer, analysis);
        }

        writer.WriteLine($"    }}");
        writer.WriteLine($"}}");

        return writer.ToString();
    }

    private static void RenderInterceptor(
        PooledStringWriter writer,
        InterpolationAnalysisResult analysis
    ) {
        analysis.InterceptsLocationAttribute.WriteTo(writer, 2);
        writer.WriteLine();
        analysis.InterceptorMethodDeclaration.WriteTo(writer, 2);
        writer.WriteLine($"        {{");

        foreach(var definition in analysis.ValueDefinitions) {
            writer.Write($"            var {definition.Identifier} = ");
            definition.Initializer.WriteTo(writer, 3);
            writer.WriteLine(";");
        }

        if(analysis.DataDeclaration is not null) {
            analysis.DataDeclaration.WriteTo(writer, 3);
            writer.WriteLine();
        }

        writer.WriteLine();
        analysis.ReturnStatement.WriteTo(writer, 3);
        writer.WriteLine();

        foreach(var definition in analysis.MethodDefinitions) {
            writer.WriteLine();
            definition.WriteTo(writer, 3);
            writer.WriteLine();
        }

        writer.WriteLine($"        }}");
    }
}
