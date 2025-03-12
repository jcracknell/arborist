namespace Arborist.Interpolation.Internal;

/// <summary>
/// The default <see cref="ISplicedExpressionEvaluator"/> implementation, which attempts to evaluate
/// expressions using the configured <see cref="IPartialSplicedExpressionEvaluator"/> before using
/// the provided <see cref="ISplicedExpressionEvaluator"/> implementation as a fallback.
/// </summary>
public class DefaultSplicedExpressionEvaluator(
    IPartialSplicedExpressionEvaluator partialExpressionEvaluator,
    ISplicedExpressionEvaluator expressionEvaluator
)
    : ISplicedExpressionEvaluator
{
    public IReadOnlyList<object?> Evaluate<TData>(SplicedExpressionEvaluationContext<TData> context) {
        var expressionCount = context.Expressions.Count;

        // First we attempt to evaluate the expressions using the partial evaluator, as this is presumably
        // faster than the accompanying total evaluator.
        var evaluated = TryEvaluatePartial(context, out var evaluatedCount);
        if(evaluatedCount == expressionCount)
            return evaluated;

        var fallbackExpressions = new Expression[expressionCount - evaluatedCount];
        for(var i = 0; i < fallbackExpressions.Length; i++)
            fallbackExpressions[i] = context.Expressions[evaluatedCount + i];

        var fallbackValues = expressionEvaluator.Evaluate<TData>(new(
            data: context.Data,
            dataReferences: context.DataReferences,
            expressions: fallbackExpressions
        ));

        for(var i = 0; i < fallbackValues.Count; i++)
            evaluated[evaluatedCount + i] = fallbackValues[i];

        return evaluated;
    }

    private object?[] TryEvaluatePartial<TData>(SplicedExpressionEvaluationContext<TData> context, out int evaluatedCount) {
        evaluatedCount = 0;

        var expressionCount = context.Expressions.Count;
        if(expressionCount == 0)
            return Array.Empty<object?>();

        var evaluated = new object?[expressionCount];
        while(evaluatedCount < expressionCount) {
            var expression = context.Expressions[evaluatedCount];

            try {
                // In the event of a failure, we have to give up to ensure that the expressions are evaluated
                // in the expected order.
                if(!partialExpressionEvaluator.TryEvaluate(context.Data, expression, out var value))
                    break;

                evaluated[evaluatedCount] = value;
                evaluatedCount += 1;
            } catch(Exception ex) {
                throw new SpliceArgumentEvaluationException(expression, ex);
            }
        }

        return evaluated;
    }
}
