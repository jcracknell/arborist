namespace Arborist.Interpolation.Internal;

/// <summary>
/// Represents a service which may be able to evaluate a subset of possible spliced expressions.
/// </summary>
public interface IPartialSplicedExpressionEvaluator {
    public bool TryEvaluate<TData>(PartialSplicedExpressionEvaluationContext<TData> context, out object? value);
}
