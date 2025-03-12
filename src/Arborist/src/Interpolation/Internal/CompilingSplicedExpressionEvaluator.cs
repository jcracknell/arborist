using Arborist.Internal.Collections;

namespace Arborist.Interpolation.Internal;

public class CompilingSplicedExpressionEvaluator(IExpressionCompiler expressionCompiler)
    : ISplicedExpressionEvaluator
{
    public IReadOnlyList<object?> Evaluate<TData>(SplicedExpressionEvaluationContext<TData> context) {
        var evaluator = CompileEvaluator(context);

        // Only wrap exceptions occurring as a result of the invocation, as those are (probably) the
        // caller's fault.
        try {
            return evaluator(context.Data);
        } catch(Exception ex) {
            throw new SpliceArgumentEvaluationException(ex);
        }
    }

    private Func<TData, object?[]> CompileEvaluator<TData>(SplicedExpressionEvaluationContext<TData> context) {
        var dataParameter = Expression.Parameter(typeof(TData));

        // Create an expression evaluating all of the provided expressions into an array
        var expression = Expression.Lambda<Func<TData, object?[]>>(
            ExpressionHelper.Replace(
                Expression.NewArrayInit(typeof(object),
                    from expr in context.Expressions
                    select Expression.Convert(expr, typeof(object))
                ),
                // Replace references to IInterpolationContext.Data with the data parameter
                SmallDictionary.CreateRange(
                    from dataReference in context.DataReferences
                    select new KeyValuePair<Expression, Expression>(dataReference, dataParameter)
                )
            ),
            dataParameter
        );

        return expressionCompiler.Compile(expression);
    }
}
