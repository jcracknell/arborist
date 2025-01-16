using Arborist.Internal.Collections;
using Arborist.Interpolation.Internal;

namespace Arborist;

public static partial class ExpressionHelper {
    internal static Expression<TDelegate> InterpolateCore<TData, TDelegate>(TData data, LambdaExpression expression)
        where TDelegate : Delegate
    {
        var analyzer = new AnalyzingInterpolationVisitor(expression);
        analyzer.Apply(expression.Body);

        var parameterExpressions = expression.Parameters.Skip(1);

        var interpolator = new SplicingInterpolationVisitor(
            evaluatedSpliceParameters: EvaluateInterpolatedExpressions(
                data: data,
                evaluatedExpressions: analyzer.EvaluatedExpressions,
                dataReferences: analyzer.DataReferences
            )
        );

        return Expression.Lambda<TDelegate>(
            body: interpolator.Apply(expression.Body),
            parameters: expression.Parameters.Skip(1)
        );
    }

    private static IReadOnlyList<object?> EvaluateInterpolatedExpressions<TData>(
        TData data,
        IReadOnlyList<Expression> evaluatedExpressions,
        IReadOnlySet<MemberExpression> dataReferences
    ) {
        if(evaluatedExpressions.Count == 0)
            return Array.Empty<object?>();

        var unevaluatedExpressions = default(List<(int Index, Expression Expression)>);
        var values = new object?[evaluatedExpressions.Count];
        for(var i = 0; i < evaluatedExpressions.Count; i++) {
            switch(evaluatedExpressions[i]) {
                // These are constant values, so removing them from the evaluation process has no
                // side effects.
                case ConstantExpression { Value: var value }:
                    values[i] = value;
                    break;
                case UnaryExpression { NodeType: ExpressionType.Convert, Operand: ConstantExpression { Value: var value } }:
                    values[i] = value;
                    break;
                case var unevaluated:
                    unevaluatedExpressions ??= new(evaluatedExpressions.Count - i);
                    unevaluatedExpressions.Add((i, unevaluated));
                    break;
            }
        }

        // If there are no expressions requiring evaluation, then we can skip costly evaluation
        if(unevaluatedExpressions is not { Count: not 0 })
            return values;

        var dataParameter = Expression.Parameter(typeof(TData));

        // Build a dictionary mapping references to IInterpolationContext.Data to the data parameter.
        // We know each reference is unique because this is a set of expressions.
        var dataReferenceReplacements = SmallDictionary.CreateRange(
            from dataReference in dataReferences
            select new KeyValuePair<Expression, Expression>(dataReference, dataParameter)
        );

        // Create an expression to evaluate the unevaluated expression values into an array
        var evaluatedValues = Expression.Lambda<Func<TData, object?[]>>(
            Expression.NewArrayInit(typeof(object),
                from tup in unevaluatedExpressions select Expression.Convert(
                    ExpressionHelper.Replace(tup.Expression, dataReferenceReplacements),
                    typeof(object)
                )
            ),
            dataParameter
        )
        .Compile()
        .Invoke(data);

        // Fill the holes in the result array by mapping each evaluated value to the index of the
        // corresponding unevaluated expression
        for(var i = 0; i < evaluatedValues.Length; i++)
            values[unevaluatedExpressions[i].Index] = evaluatedValues[i];

        return values;
    }
}
