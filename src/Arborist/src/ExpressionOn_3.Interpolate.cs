using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public static partial class ExpressionOn<A, B, C> {
    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [InterceptedExpressionInterpolator]
    public static Expression<Func<A, B, C, R>> Interpolate<R>(
        Expression<Func<IInterpolationContext, A, B, C, R>> expression
    ) =>
        ExpressionInterpolator.Interpolate<object?, Func<A, B, C, R>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [InterceptedExpressionInterpolator]
    public static Expression<Action<A, B, C>> Interpolate(
        Expression<Action<IInterpolationContext, A, B, C>> expression
    ) =>
        ExpressionInterpolator.Interpolate<object?, Action<A, B, C>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext{TData}"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <param name="data">
    /// Data provided to the interpolation process, accessible via the <see cref="IInterpolationContext{TData}.Data"/>
    /// property of the <see cref="IInterpolationContext{TData}"/> argument.
    /// </param>
    /// <typeparam name="TData">
    /// The type of the data provided to the interpolation process.
    /// </typeparam>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [InterceptedExpressionInterpolator]
    public static Expression<Func<A, B, C, R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, B, C, R>> expression
    ) =>
        ExpressionInterpolator.Interpolate<TData, Func<A, B, C, R>>(data, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext{TData}"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <param name="data">
    /// Data provided to the interpolation process, accessible via the <see cref="IInterpolationContext{TData}.Data"/>
    /// property of the <see cref="IInterpolationContext{TData}"/> argument.
    /// </param>
    /// <typeparam name="TData">
    /// The type of the data provided to the interpolation process.
    /// </typeparam>
    [InterceptedExpressionInterpolator]
    public static Expression<Action<A, B, C>> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A, B, C>> expression
    ) =>
        ExpressionInterpolator.Interpolate<TData, Action<A, B, C>>(data, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <remarks>
    /// This method is an escape hatch providing explicit access to the runtime interpolation
    /// implementation, and can be used as a workaround for potential bugs or deficiencies in
    /// the compile time interpolator.
    /// </remarks>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [RuntimeExpressionInterpolator]
    public static Expression<Func<A, B, C, R>> InterpolateRuntimeFallback<R>(
        Expression<Func<IInterpolationContext, A, B, C, R>> expression
    ) =>
        ExpressionInterpolator.Interpolate<object?, Func<A, B, C, R>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <remarks>
    /// This method is an escape hatch providing explicit access to the runtime interpolation
    /// implementation, and can be used as a workaround for potential bugs or deficiencies in
    /// the compile time interpolator.
    /// </remarks>
    [RuntimeExpressionInterpolator]
    public static Expression<Action<A, B, C>> InterpolateRuntimeFallback(
        Expression<Action<IInterpolationContext, A, B, C>> expression
    ) =>
        ExpressionInterpolator.Interpolate<object?, Action<A, B, C>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext{TData}"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <remarks>
    /// This method is an escape hatch providing explicit access to the runtime interpolation
    /// implementation, and can be used as a workaround for potential bugs or deficiencies in
    /// the compile time interpolator.
    /// </remarks>
    /// <param name="data">
    /// Data provided to the interpolation process, accessible via the <see cref="IInterpolationContext{TData}.Data"/>
    /// property of the <see cref="IInterpolationContext{TData}"/> argument.
    /// </param>
    /// <typeparam name="TData">
    /// The type of the data provided to the interpolation process.
    /// </typeparam>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [RuntimeExpressionInterpolator]
    public static Expression<Func<A, B, C, R>> InterpolateRuntimeFallback<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, B, C, R>> expression
    ) =>
        ExpressionInterpolator.Interpolate<TData, Func<A, B, C, R>>(data, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext{TData}"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <remarks>
    /// This method is an escape hatch providing explicit access to the runtime interpolation
    /// implementation, and can be used as a workaround for potential bugs or deficiencies in
    /// the compile time interpolator.
    /// </remarks>
    /// <param name="data">
    /// Data provided to the interpolation process, accessible via the <see cref="IInterpolationContext{TData}.Data"/>
    /// property of the <see cref="IInterpolationContext{TData}"/> argument.
    /// </param>
    /// <typeparam name="TData">
    /// The type of the data provided to the interpolation process.
    /// </typeparam>
    [RuntimeExpressionInterpolator]
    public static Expression<Action<A, B, C>> InterpolateRuntimeFallback<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A, B, C>> expression
    ) =>
        ExpressionInterpolator.Interpolate<TData, Action<A, B, C>>(data, expression);

}
