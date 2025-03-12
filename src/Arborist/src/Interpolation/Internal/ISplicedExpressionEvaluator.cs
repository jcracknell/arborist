namespace Arborist.Interpolation.Internal;

public interface ISplicedExpressionEvaluator {
    public IReadOnlyList<object?> Evaluate<TData>(SplicedExpressionEvaluationContext<TData> context);
}
