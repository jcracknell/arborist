using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public static class ExpressionOn<A, B, C, D> {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, B, C, D, R>> Of<R>(Expression<Func<A, B, C, D, R>> expression) =>
        expression;

    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A, B, C, D>> Of(Expression<Action<A, B, C, D>> expression) =>
        expression;

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, RR>> Graft<R, RR>(
        Expression<Func<A, B, C, D, R>> root,
        Expression<Func<R, RR>> branch
    ) =>
        Expression.Lambda<Func<A, B, C, D, RR>>(
            body: ExpressionHelper.Replace(branch.Body, branch.Parameters[0], root.Body),
            parameters: root.Parameters
        );

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
    [CompileTimeExpressionInterpolator]
    public static Expression<Func<A, B, C, D, R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, B, C, D, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Func<A, B, C, D, R>>(data, expression);

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
    [CompileTimeExpressionInterpolator]
    public static Expression<Action<A, B, C, D>> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A, B, C, D>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Action<A, B, C, D>>(data, expression);

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
    public static Expression<Func<A, B, C, D, R>> InterpolateRuntimeFallback<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, B, C, D, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Func<A, B, C, D, R>>(data, expression);

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
    public static Expression<Action<A, B, C, D>> InterpolateRuntimeFallback<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A, B, C, D>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Action<A, B, C, D>>(data, expression);
}
