using Arborist.Interpolation.Internal;

namespace Arborist;

public static partial class ExpressionHelper {
    internal static Expression<TDelegate> InterpolateCore<TData, TDelegate>(TData data, LambdaExpression expression)
        where TDelegate : Delegate
    {
        var analyzer = new AnalyzingInterpolationVisitor(expression);
        analyzer.Visit(expression.Body);

        var parameterExpressions = expression.Parameters.Skip(1);

        var interpolator = new SplicingInterpolationVisitor(
            evaluatedSpliceParameters: EvaluateInterpolatedExpressions(
                data: data,
                evaluatedExpressions: analyzer.EvaluatedExpressions,
                dataReferences: analyzer.DataReferences
            )
        );

        return Expression.Lambda<TDelegate>(
            body: interpolator.Visit(expression.Body),
            parameters: expression.Parameters.Skip(1)
        );
    }

    private static IReadOnlyDictionary<Expression, object?> EvaluateInterpolatedExpressions<TData>(
        TData data,
        IReadOnlySet<Expression> evaluatedExpressions,
        IReadOnlySet<MemberExpression> dataReferences
    ) {
        if(evaluatedExpressions.Count == 0)
            return ImmutableDictionary<Expression, object?>.Empty;

        var unevaluatedExpressions = default(List<Expression>);
        var evaluatedValues = new Dictionary<Expression, object?>(evaluatedExpressions.Count);
        foreach(var expr in evaluatedExpressions) {
            switch(expr) {
                case ConstantExpression { Value: var value }:
                    evaluatedValues[expr] = value;
                    break;
                case UnaryExpression { NodeType: ExpressionType.Convert, Operand: ConstantExpression { Value: var value } }:
                    evaluatedValues[expr] = value;
                    break;
                default:
                    (unevaluatedExpressions ??= new(evaluatedExpressions.Count - evaluatedValues.Count)).Add(expr);
                    break;
            }
        }

        // If there are no expressions requiring evaluation, then we can skip costly evaluation
        if(unevaluatedExpressions is not { Count: not 0 })
            return evaluatedValues;

        var dataParameter = Expression.Parameter(typeof(TData));

        // Build a dictionary mapping references to ISplicingContext.Data with the data parameter
        var dataReferenceReplacements = new Dictionary<Expression, Expression>(dataReferences.Count);
        foreach(var dataReference in dataReferences)
            dataReferenceReplacements[dataReference] = dataParameter;

        var evaluated = Expression.Lambda<Func<TData, object?[]>>(
            Expression.NewArrayInit(typeof(object),
                from expr in unevaluatedExpressions select Expression.Convert(
                    ExpressionHelper.Replace(expr, dataReferenceReplacements),
                    typeof(object)
                )
            ),
            dataParameter
        )
        .Compile()
        .Invoke(data);

        for(var i = 0; i < unevaluatedExpressions.Count; i++)
            evaluatedValues[unevaluatedExpressions[i]] = evaluated[i];

        return evaluatedValues;
    }

}
