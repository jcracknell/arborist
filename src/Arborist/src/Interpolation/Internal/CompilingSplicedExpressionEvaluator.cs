using Arborist.Internal;
using Arborist.Internal.Collections;

namespace Arborist.Interpolation.Internal;

public class CompilingSplicedExpressionEvaluator(ISplicedExpressionCompiler splicedExpressionCompiler)
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
                Expression.NewArrayInit(typeof(object), CollectionHelpers.SelectEager(
                    context.Expressions,
                    static expr => (Expression)Expression.Convert(expr, typeof(object))
                )),
                // Replace references to IInterpolationContext.Data with the data parameter
                SmallDictionary.CreateRange(CollectionHelpers.SelectEager(
                    context.DataReferences,
                    dataReference => new KeyValuePair<Expression, Expression>(dataReference, dataParameter)
                ))
            ),
            dataParameter
        );

        return splicedExpressionCompiler.Compile(expression);
    }
}
