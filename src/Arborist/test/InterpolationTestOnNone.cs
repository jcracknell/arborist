using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public static class InterpolationTestOnNone {
    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Func<R>> Interpolate<R>(
        Expression<Func<IInterpolationContext, R>> expression
    ) =>
        throw new NotImplementedException();

    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Action> Interpolate(
        Expression<Action<IInterpolationContext>> expression
    ) =>
        throw new NotImplementedException();

    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Func<R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, R>> expression
    ) =>
        throw new NotImplementedException();

    /// <summary>
    /// Expression interpolator provided for unit-testing purposes.
    /// Generates errors in the event that the source generator fails to create an interceptor for any reason.
    /// </summary>
    [InterceptedExpressionInterpolator(InterceptionRequired = true)]
    public static Expression<Action> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>>> expression
    ) =>
        throw new NotImplementedException();
}
