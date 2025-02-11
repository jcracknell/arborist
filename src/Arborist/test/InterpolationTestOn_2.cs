using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public static class InterpolationTestOn<A, B> {
    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Func<A, B, R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, B, R>> expression
    ) =>
        throw new NotImplementedException();

    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Action<A, B>> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A, B>> expression
    ) =>
        throw new NotImplementedException();
}
