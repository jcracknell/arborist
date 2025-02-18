using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

internal static class SyntaxHelpers {
    /// <summary>
    /// Determines if the provided <paramref name="node"/> exists in a checked evaluation context by
    /// checking for the most recent enclosing <see cref="CheckedExpressionSyntax"/> or
    /// <see cref="CheckedStatementSyntax"/>, or failing that eventually returning the value of
    /// <see cref="CompilationOptions"/>.<see cref="CompilationOptions.CheckOverflow"/>
    /// </summary>
    public static bool InCheckedContext(SyntaxNode node, SemanticModel semanticModel) =>
        node.Kind() switch {
            SyntaxKind.CheckedExpression => true,
            SyntaxKind.UncheckedExpression => false,
            SyntaxKind.CheckedStatement => true,
            SyntaxKind.UncheckedStatement => false,
            _ => node.Parent switch {
                null => semanticModel.Compilation.Options.CheckOverflow,
                var parent => InCheckedContext(parent, semanticModel)
            }
        };

    /// <summary>
    /// Returns true if the provided <paramref name="node"/> represents a call to a generic method
    /// where the type parameters have been explicitly specified.
    /// </summary>
    public static bool IsExplicitGenericMethodInvocation([NotNullWhen(true)] SyntaxNode? node) =>
        node is InvocationExpressionSyntax {
            Expression: GenericNameSyntax or MemberAccessExpressionSyntax { Name: GenericNameSyntax }
        };
}
