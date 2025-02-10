namespace Arborist.Interpolation.Internal;

public sealed class InterceptedExpressionInterpolatorAttribute : ExpressionInterpolatorAttribute {
    /// <summary>
    /// Signals that this expression interpolation method must be intercepted, and failure to generate
    /// an interceptor should result in a compiler error.
    /// </summary>
    public bool InterceptionRequired { get; init; } = false;
}
