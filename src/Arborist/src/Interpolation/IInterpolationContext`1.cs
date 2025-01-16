using Arborist.Interpolation.Internal;

namespace Arborist.Interpolation;

/// <summary>
/// Context for the expression interpolation process. Provides access to subtree splicing methods,
/// and data to be used in the resulting expression.
/// </summary>
/// <typeparam name="TData">
/// The type carrying the data provided to the interpolation process.
/// </typeparam>
public interface IInterpolationContext<out TData> : IInterpolationContext {
    /// <summary>
    /// The data provided to the interpolation process. May be referenced within evaluated
    /// splice parameters (annotated with <see cref="EvaluatedSpliceParameterAttribute"/>).
    /// </summary>
    public TData Data =>
        throw InterpolationContextEvaluationException.Evaluated(typeof(IInterpolationContext<TData>).GetProperty(nameof(Data))!);
}
