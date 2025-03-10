using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public static partial class ExpressionOn<A, B, C, D> {
    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [ExpressionInterpolator]
    public static Expression<Func<A, B, C, D, R>> Interpolate<R>(
        Expression<Func<IInterpolationContext, A, B, C, D, R>> expression
    ) =>
        ExpressionInterpolator.Interpolate<object?, Func<A, B, C, D, R>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [ExpressionInterpolator]
    public static Expression<Action<A, B, C, D>> Interpolate(
        Expression<Action<IInterpolationContext, A, B, C, D>> expression
    ) =>
        ExpressionInterpolator.Interpolate<object?, Action<A, B, C, D>>(default, expression);

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
    public static Expression<Func<A, B, C, D, R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, B, C, D, R>> expression
    ) =>
        ExpressionInterpolator.Interpolate<TData, Func<A, B, C, D, R>>(data, expression);

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
    public static Expression<Action<A, B, C, D>> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A, B, C, D>> expression
    ) =>
        ExpressionInterpolator.Interpolate<TData, Action<A, B, C, D>>(data, expression);
}
