using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Reflection;

namespace Arborist.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InterpolationAnalyzer : DiagnosticAnalyzer {
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
        if(!TryGetInvocationMethodIdentifier(invocation, out var identifier))
            return;
        if(!identifier.ValueText.Contains("Interpolate"))
            return;
        if(context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
            return;
        if(!methodSymbol.GetAttributes().Any(IsExpressionInterpolatorAttribute))
            return;

        var typeSymbols = InterpolationTypeSymbols.Create(context.SemanticModel.Compilation);
        if(!SymbolHelpers.HasAttribute(methodSymbol, typeSymbols.ExpressionInterpolatorAttribute))
            return;

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
            Name: "ExpressionInterpolatorAttribute",
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

    private static void AnalyzeInvocation(
        InterpolationDiagnosticsCollection diagnostics,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol,
        InterpolationTypeSymbols typeSymbols,
        CancellationToken cancellationToken
    ) {
        foreach(var parameter in methodSymbol.Parameters) {
            if(!IsInterpolatedExpressionParameter(parameter, typeSymbols))
                continue;
            if(!TryGetParameterArgumentSyntax(invocation, parameter, out var expressionArgument, semanticModel))
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

    private static bool IsInterpolatedExpressionParameter(
        IParameterSymbol parameter,
        InterpolationTypeSymbols typeSymbols
    ) {
        if(parameter.Type is not INamedTypeSymbol parameterType)
            return false;
        if(!parameterType.IsGenericType)
            return false;
        if(!SymbolEqualityComparer.Default.Equals(parameterType.ConstructUnboundGenericType(), typeSymbols.Expression1.ConstructUnboundGenericType()))
            return false;
        if(parameterType.TypeArguments[0] is not INamedTypeSymbol { IsGenericType: true } interpolatedDelegateType)
            return false;
        if(!SymbolHelpers.IsSubtype(interpolatedDelegateType.TypeArguments[0], typeSymbols.IInterpolationContext))
            return false;
        // Has [InterpolatedExpressionParameter]
        if(!SymbolHelpers.HasAttribute(parameter, typeSymbols.InterpolatedExpressionParameterAttribute))
            return false;

        return true;
    }

    /// <summary>
    /// Attempts to locate the argument tree in the provided invocation corresponding to the provided
    /// <see cref="IParameterSymbol"/>.
    /// </summary>
    private static bool TryGetParameterArgumentSyntax(
        InvocationExpressionSyntax invocation,
        IParameterSymbol parameterSymbol,
        [NotNullWhen(true)] out ArgumentSyntax? argumentSyntax,
        SemanticModel semanticModel
    ) {
        // The semantic model is based on the "unreduced" versions of extension method parameters
        var expandedParameter = parameterSymbol.ContainingSymbol switch {
            IMethodSymbol { ReducedFrom: not null } extension =>
                extension.GetConstructedReducedFrom()!.Parameters[parameterSymbol.Ordinal + 1],
            _ => parameterSymbol
        };

        // In the vast majority of cases the parameter will be specified positionally, so we'll
        // try that first
        var positionalArgument = invocation.ArgumentList.Arguments[parameterSymbol.Ordinal];
        if(
            semanticModel.GetOperation(positionalArgument) is IArgumentOperation pop
            && SymbolEqualityComparer.Default.Equals(pop.Parameter, expandedParameter)
        ) {
            argumentSyntax = positionalArgument;
            return true;
        }

        foreach(var namedArgument in invocation.ArgumentList.Arguments) {
            if(namedArgument.NameColon is null)
                continue;
            if(semanticModel.GetOperation(namedArgument) is not IArgumentOperation nop)
                continue;
            if(SymbolEqualityComparer.Default.Equals(nop.Parameter, expandedParameter)) {
                argumentSyntax = namedArgument;
                return true;
            }
        }

        argumentSyntax = default;
        return false;
    }
}
