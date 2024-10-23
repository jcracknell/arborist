using Arborist.Internal;
using System.Collections.Immutable;

namespace Arborist;

public static partial class ExpressionHelpers {
    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on <see cref="EI"/> with the corresponding subexpressions.
    /// </summary>
    /// <seealso cref="EI"/>
    public static Expression<TDelegate> Interpolate<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate
    {
        var analyzer = new AnalyzingInterpolationVisitor(expression);
        analyzer.Visit(expression.Body);

        var evaluatedExpressions = EvaluateInterpolatedExpressions(analyzer.EvaluatedExpressions);
        if(evaluatedExpressions.Count == 0)
            return expression;

        var interpolator = new SplicingInterpolationVisitor(evaluatedExpressions);

        return Expression.Lambda<TDelegate>(
            body: interpolator.Visit(expression.Body),
            parameters: expression.Parameters
        );
    }

    private static IReadOnlyDictionary<Expression, object?> EvaluateInterpolatedExpressions(
        IReadOnlySet<Expression> expressions
    ) {
        if(expressions.Count == 0)
            return ImmutableDictionary<Expression, object?>.Empty;

        var pending = default(List<Expression>);
        var result = new Dictionary<Expression, object?>(expressions.Count);
        foreach(var expr in expressions) {
            switch(expr) {
                case ConstantExpression { Value: var value }:
                    result[expr] = value;
                    break;
                case UnaryExpression { NodeType: ExpressionType.Convert, Operand: ConstantExpression { Value: var value } }:
                    result[expr] = value;
                    break;
                default:
                    (pending ??= new(expressions.Count - result.Count)).Add(expr);
                    break;
            }
        }

        // If there are no expressions requiring evaluation, then we can skip costly evaluation
        if(pending is null)
            return result;

        var evaluated = Expression.Lambda<Func<object?[]>>(
            Expression.NewArrayInit(
                typeof(object),
                pending.Select(expr => Expression.Convert(expr, typeof(object)))
            )
        )
        .Compile()
        .Invoke();

        for(var i = 0; i < pending.Count; i++)
            result[pending[i]] = evaluated[i];

        return result;
    }
}
