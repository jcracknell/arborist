using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Arborist.Analyzers;

internal static class SyntaxHelpers {
    public static bool IsExpressionInterpolatorInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out IMethodSymbol? methodSymbol
    ) {
        methodSymbol = default;

        if(!TryGetInvocationMethodIdentifier(invocation, out var identifier))
            return false;
        if(!identifier.ValueText.Contains("Interpolate"))
            return false;

        methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if(methodSymbol is null)
            return false;
        if(!methodSymbol.GetAttributes().Any(IsExpressionInterpolatorAttribute))
            return false;

        var expressionInterpolatorAttribute = semanticModel.Compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.ExpressionInterpolatorAttribute")!;
        if(!SymbolHelpers.HasAttribute(methodSymbol, expressionInterpolatorAttribute))
            return false;

        return true;
    }

    private static bool TryGetInvocationMethodIdentifier(InvocationExpressionSyntax invocation, out SyntaxToken identifier) {
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

    /// <summary>
    /// Attempts to locate the argument tree in the provided invocation corresponding to the provided
    /// <see cref="IParameterSymbol"/>.
    /// </summary>
    public static bool TryGetParameterArgumentSyntax(
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
