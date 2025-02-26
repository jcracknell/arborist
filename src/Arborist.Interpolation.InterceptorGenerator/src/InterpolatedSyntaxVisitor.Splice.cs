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

        if(!SymbolHelpers.IsSubtype(methodSymbol.ContainingType, _context.TypeSymbols.IInterpolationContext))
            return false;

        splicingMethod = methodSymbol;
        return true;
    }

    private InterpolatedTree VisitSplicingInvocation(InvocationExpressionSyntax node, IMethodSymbol method) {
        CurrentExpr.SetType(typeof(MethodCallExpression));

        return method.Name switch {
            "Splice" => VisitSplice(node, method),
            "SpliceBody" => VisitSpliceBody(node, method),
            "SpliceValue" => VisitSpliceValue(node, method),
            "SpliceQuoted" => VisitSpliceQuoted(node, method),
            _ => _context.Diagnostics.UnsupportedInterpolatedSyntax(node)
        };
    }

    private InterpolatedTree VisitSplice(InvocationExpressionSyntax node, IMethodSymbol method) {
        var expressionArgument = node.ArgumentList.Arguments[0];
        var identifier = _context.TreeBuilder.CreateIdentifier();

        return InterpolatedTree.Switch(
            CurrentExpr.BindCallArg(method, 1).WithValue(VisitSplicedExpression(expressionArgument, method, 1)),
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
        // Generate the interpolated parameter trees so that the nodes are evaluated in the declared order
        var bindings = new KeyValuePair<string, InterpolatedTree>[method.Parameters.Length];
        for(var i = 0; i < method.Parameters.Length - 1; i++)
            bindings[i] = new(
                key: _builder.CreateIdentifier(),
                value: CurrentExpr.BindCallArg(method, i + 1).WithValue(Visit(node.ArgumentList.Arguments[i]))
            );

        // Bind the evaluated spliced expression
        var expressionNode = node.ArgumentList.Arguments[method.Parameters.Length - 1];
        var expressionIdentifier = _builder.CreateIdentifier();
        bindings[method.Parameters.Length - 1] = new(
            key: expressionIdentifier,
            value: CurrentExpr.BindCallArg(method, method.Parameters.Length)
            .WithValue(VisitSplicedExpression(expressionNode, method, method.Parameters.Length))
        );

        var replacements = new InterpolatedTree[method.Parameters.Length - 1];
        for(var i = 0; i < method.Parameters.Length - 1; i++)
            replacements[i] = InterpolatedTree.Call(
                InterpolatedTree.Interpolate($"new global::System.Collections.Generic.KeyValuePair<{_builder.ExpressionTypeName}, {_builder.ExpressionTypeName}>"),
                [
                    InterpolatedTree.Verbatim($"{expressionIdentifier}.{nameof(LambdaExpression.Parameters)}[{i}]"),
                    InterpolatedTree.Verbatim(bindings[i].Key)
                ]
            );

        return InterpolatedTree.BindTuple(
            bindings,
            method.Parameters.Length switch {
                // There are no parameters requiring replacement in the case of e.g. an Expression<Func<A>>,
                // in which case we just embed the body of the expression verbatim
                <= 1 => InterpolatedTree.Interpolate($"{expressionIdentifier}.{nameof(LambdaExpression.Body)}"),
                // Otherwise we need to replace occurrences of the parameters in the spliced expression body
                _ => InterpolatedTree.Call(InterpolatedTree.Verbatim("global::Arborist.ExpressionHelper.Replace"), [
                    InterpolatedTree.Interpolate($"{expressionIdentifier}.{nameof(LambdaExpression.Body)}"),
                    InterpolatedTree.Call(
                        InterpolatedTree.Verbatim("global::Arborist.Internal.Collections.SmallDictionary.Create"),
                        replacements
                    )
                ])
            }
        );
    }

    private InterpolatedTree VisitSplicedExpression(ArgumentSyntax node, IMethodSymbol method, int parameterIndex) {
        // If this is not a lambda literal, we can return the resulting lambda directly
        if(node.Expression is not LambdaExpressionSyntax)
            return VisitEvaluatedSyntax(node.Expression);

        // Otherwise we need to provide the target expression type for the lambda
        var expressionType = SymbolHelpers.GetParameterType(method, parameterIndex);
        var expressionTypeRef = _builder.CreateTypeRef(expressionType, node);

        return InterpolatedTree.Call(
            InterpolatedTree.Interpolate($"{expressionTypeRef}.Coerce"),
            [VisitEvaluatedSyntax(node.Expression)]
        );
    }

    private InterpolatedTree VisitSpliceValue(InvocationExpressionSyntax node, IMethodSymbol method) {
        var valueNode = node.ArgumentList.Arguments[0].Expression;

        return _builder.CreateExpression(nameof(Expression.Constant),
            CurrentExpr.BindCallArg(method, 1).WithValue(VisitEvaluatedSyntax(valueNode)),
            CurrentExpr.BindValue($"{nameof(MethodCallExpression.Type)}")
        );
    }

    private InterpolatedTree VisitSpliceQuoted(InvocationExpressionSyntax node, IMethodSymbol method) {
        var expressionNode = node.ArgumentList.Arguments[0].Expression;

        return _builder.CreateExpression(
            nameof(Expression.Quote),
            CurrentExpr.BindCallArg(method, 1).WithValue(VisitEvaluatedSyntax(expressionNode))
        );
    }
}
