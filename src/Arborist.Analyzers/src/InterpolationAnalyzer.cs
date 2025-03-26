using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Reflection;

namespace Arborist.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InterpolationAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = GetSupportedDiagnostics();

    private static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics() =>
        typeof(InterpolationDiagnosticDescriptors)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
        .Select(p => (DiagnosticDescriptor)p.GetValue(null))
        .ToImmutableArray();

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.InvocationExpression);
    }

    private static void SyntaxNodeAction(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if(!SyntaxHelpers.IsExpressionInterpolatorInvocation(invocation, context.SemanticModel, out var methodSymbol))
            return;

        var typeSymbols = InterpolationTypeSymbols.Create(context.SemanticModel.Compilation);
        var diagnostics = new InterpolationDiagnosticsCollection();

        AnalyzeInvocation(
            diagnostics: diagnostics,
            semanticModel: context.SemanticModel,
            invocation: invocation,
            methodSymbol: methodSymbol,
            typeSymbols: typeSymbols,
            cancellationToken: context.CancellationToken
        );

        foreach(var diagnostic in diagnostics)
            context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeInvocation(
        InterpolationDiagnosticsCollection diagnostics,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol,
        InterpolationTypeSymbols typeSymbols,
        CancellationToken cancellationToken
    ) {
        foreach(var parameter in methodSymbol.Parameters) {
            if(!SymbolHelpers.IsInterpolatedExpressionParameter(parameter, typeSymbols))
                continue;
            if(!SyntaxHelpers.TryGetParameterArgumentSyntax(invocation, parameter, out var expressionArgument, semanticModel))
                continue;

            AnalyzeInterpolatedExpression(
                semanticModel: semanticModel,
                diagnostics: diagnostics,
                expressionArgument: expressionArgument,
                typeSymbols: typeSymbols,
                cancellationToken: cancellationToken
            );
        }
    }

    private static void AnalyzeInterpolatedExpression(
        InterpolationDiagnosticsCollection diagnostics,
        SemanticModel semanticModel,
        ArgumentSyntax expressionArgument,
        InterpolationTypeSymbols typeSymbols,
        CancellationToken cancellationToken
    ) {
        // If the expression is not provided as a literal lambda expression, we cannot perform the analysis
        if(expressionArgument.Expression is not LambdaExpressionSyntax lambdaSyntax)
            return;

        var context = new InterpolationAnalysisContext(
            semanticModel: semanticModel,
            typeSymbols: typeSymbols,
            diagnostics: diagnostics,
            cancellationToken: cancellationToken
        );

        var visitor = new InterpolationAnalysisSyntaxWalker(context);
        visitor.Apply(lambdaSyntax);

        // Report an interpolated expression containing no splices, which is completely useless.
        // In this case we still emit the interceptor, as this will be more performant than having the runtime
        // fallback rewrite the expression.
        if(!visitor.SplicesFound)
            diagnostics.ReportNoSplices(lambdaSyntax);
    }
}
