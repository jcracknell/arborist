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
    [ExpressionInterpolator]
    public static Expression<Func<A, B, C, R>> Interpolate<R>(
        [InterpolatedExpressionParameter] Expression<Func<IInterpolationContext, A, B, C, R>> expression
    ) =>
        ExpressionInterpolator.Default.Interpolate<object?, Func<A, B, C, R>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [ExpressionInterpolator]
    public static Expression<Action<A, B, C>> Interpolate(
        [InterpolatedExpressionParameter] Expression<Action<IInterpolationContext, A, B, C>> expression
    ) =>
        ExpressionInterpolator.Default.Interpolate<object?, Action<A, B, C>>(default, expression);

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
    [ExpressionInterpolator]
    public static Expression<Func<A, B, C, R>> Interpolate<TData, R>(
        [InterpolatedDataParameter] TData data,
        [InterpolatedExpressionParameter] Expression<Func<IInterpolationContext<TData>, A, B, C, R>> expression
    ) =>
        ExpressionInterpolator.Default.Interpolate<TData, Func<A, B, C, R>>(data, expression);

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
    [ExpressionInterpolator]
    public static Expression<Action<A, B, C>> Interpolate<TData>(
        [InterpolatedDataParameter] TData data,
        [InterpolatedExpressionParameter] Expression<Action<IInterpolationContext<TData>, A, B, C>> expression
    ) =>
        ExpressionInterpolator.Default.Interpolate<TData, Action<A, B, C>>(data, expression);
}
