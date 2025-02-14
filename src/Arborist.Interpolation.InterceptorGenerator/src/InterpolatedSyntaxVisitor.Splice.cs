using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitor {
    private bool TryGetSplicingMethod(
        InvocationExpressionSyntax node,
        [NotNullWhen(true)] out IMethodSymbol? splicingMethod
    ) {
        splicingMethod = default;
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
            return false;

        if(!TypeSymbolHelpers.IsSubtype(methodSymbol.ContainingType, _context.TypeSymbols.IInterpolationContext))
            return false;

        splicingMethod = methodSymbol;
        return true;
    }

    private InterpolatedTree VisitSplicingInvocation(InvocationExpressionSyntax node, IMethodSymbol method) {
        var spliced = method.Name switch {
            "Splice" => VisitSplice(node, method),
            "SpliceBody" => VisitSpliceBody(node, method),
            "SpliceValue" => VisitSpliceValue(node, method),
            "SpliceQuoted" => VisitSpliceQuoted(node, method),
            _ => _context.Diagnostics.UnsupportedInterpolatedSyntax(node)
        };

        return spliced.AsModified();
    }

    private InterpolatedTree VisitSplice(InvocationExpressionSyntax node, IMethodSymbol method) {
        var evaluatedNode = node.ArgumentList.Arguments[0].Expression;
        if(_context.SemanticModel.GetTypeInfo(evaluatedNode).Type is not {} evaluatedType)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var identifier = _context.TreeBuilder.CreateIdentifier();

        CurrentExpr.SetType(typeof(MethodCallExpression));
        return InterpolatedTree.Switch(
            InterpolatedTree.Call(
                InterpolatedTree.Interpolate($"{_context.TreeBuilder.CreateTypeRef(evaluatedType)}.Coerce"),
                [VisitEvaluatedSyntax(evaluatedNode)]
            ),
            [
                InterpolatedTree.SwitchCase(
                    InterpolatedTree.Interpolate($"var {identifier}"),
                    InterpolatedTree.Ternary(
                        CurrentExpr.BindValue($"{nameof(MethodCallExpression.Type)} == {identifier}.Type"),
                        InterpolatedTree.Verbatim(identifier),
                        _builder.CreateExpression(
                            SyntaxHelpers.InCheckedContext(node, _context.SemanticModel) switch {
                                true => nameof(Expression.ConvertChecked),
                                false => nameof(Expression.Convert)
                            },
                            [
                                InterpolatedTree.Verbatim(identifier),
                                CurrentExpr.BindValue($"{nameof(MethodCallExpression.Type)}")
                            ]
                        )
                    )
                )
            ]
        );
    }

    private InterpolatedTree VisitSpliceBody(InvocationExpressionSyntax node, IMethodSymbol method) {
        var identifier = _builder.CreateIdentifier();
        var parameterCount = method.Parameters.Length - 1;
        var expressionNode = node.ArgumentList.Arguments[parameterCount];

        // Generate the interpolated parameter trees so that the nodes are interpolated in
        // declaration order.
        var parameterReplacements = new InterpolatedTree[parameterCount];
        for(var i = 0; i < parameterCount; i++)
            parameterReplacements[i] = InterpolatedTree.Call(
                InterpolatedTree.Interpolate($"new global::System.Collections.Generic.KeyValuePair<{_builder.ExpressionTypeName}, {_builder.ExpressionTypeName}>"),
                [
                    InterpolatedTree.Verbatim($"{identifier}.{nameof(LambdaExpression.Parameters)}[{i}]"),
                    CurrentExpr.BindCallArg(method, i + 1).WithValue(Visit(node.ArgumentList.Arguments[i]))
                ]
            );

        var expressionTree = VisitSpliceBodyExpression(expressionNode, method);

        // We'll use a switch expression with a single case to bind the evaluated expression tree
        CurrentExpr.SetType(typeof(MethodCallExpression));
        return InterpolatedTree.Bind(
            identifier,
            expressionTree,
            parameterReplacements.Length switch {
                // There are no parameters requiring replacement in the case of e.g. an Expression<Func<A>>,
                // in which case we just embed the body of the expression verbatim
                0 => InterpolatedTree.Interpolate($"{identifier}.{nameof(LambdaExpression.Body)}"),
                // Otherwise we need to replace occurrences of the parameters in the spliced expression body
                _ => InterpolatedTree.Call(InterpolatedTree.Verbatim("global::Arborist.ExpressionHelper.Replace"), [
                    InterpolatedTree.Interpolate($"{identifier}.{nameof(LambdaExpression.Body)}"),
                    InterpolatedTree.Call(
                        InterpolatedTree.Verbatim("global::Arborist.Internal.Collections.SmallDictionary.Create"),
                        parameterReplacements
                    )
                ])
            }
        );
    }

    private InterpolatedTree VisitSpliceBodyExpression(ArgumentSyntax node, IMethodSymbol method) {
        // If this is not a lambda literal, we can return the resulting lambda directly
        if(node.Expression is not LambdaExpressionSyntax)
            return VisitEvaluatedSyntax(node.Expression);

        // Otherwise we need to provide the target expression type for the lambda
        var expressionType = method.Parameters.Last().Type;

        return InterpolatedTree.Call(
            InterpolatedTree.Interpolate($"{_builder.CreateTypeRef(expressionType)}.Coerce"),
            [VisitEvaluatedSyntax(node.Expression)]
        );
    }

    private InterpolatedTree VisitSpliceValue(InvocationExpressionSyntax node, IMethodSymbol method) {
        var valueNode = node.ArgumentList.Arguments[0].Expression;

        CurrentExpr.SetType(typeof(MethodCallExpression));
        return _builder.CreateExpression(nameof(Expression.Constant),
            VisitEvaluatedSyntax(valueNode),
            CurrentExpr.BindValue($"{nameof(MethodCallExpression.Type)}")
        );
    }

    private InterpolatedTree VisitSpliceQuoted(InvocationExpressionSyntax node, IMethodSymbol method) {
        var expressionNode = node.ArgumentList.Arguments[0].Expression;

        CurrentExpr.SetType(typeof(MethodCallExpression));
        return _builder.CreateExpression(nameof(Expression.Quote), VisitEvaluatedSyntax(expressionNode));
    }
}
