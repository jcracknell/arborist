using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public static class InterpolationTestOn<A> {
    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Func<A, R>> Interpolate<R>(
        Expression<Func<IInterpolationContext, A, R>> expression
    ) =>
        throw new NotImplementedException();

    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Action<A>> Interpolate(
        Expression<Action<IInterpolationContext, A>> expression
    ) =>
        throw new NotImplementedException();

    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Func<A, R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, R>> expression
    ) =>
        throw new NotImplementedException();

    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Action<A>> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A>> expression
    ) =>
        throw new NotImplementedException();
}
