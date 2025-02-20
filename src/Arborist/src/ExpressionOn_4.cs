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
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, B, C, D, T>> As<T>(LambdaExpression expression) =>
        ExpressionHelper.AsCore<Func<A, B, C, D, T>>(typeof(T), expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    public static Expression<Func<A, B, C, D, T>> Convert<T>(LambdaExpression expression) =>
        ExpressionHelper.ConvertCore<Func<A, B, C, D, T>>(typeof(T), expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked"/> node (or
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> if there is no defined checked
    /// conversion) of the form <c>(T)body</c>.
    /// </summary>
    public static Expression<Func<A, B, C, D, T>> ConvertChecked<T>(LambdaExpression expression) =>
        ExpressionHelper.ConvertCheckedCore<Func<A, B, C, D, T>>(typeof(T), expression);

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
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [InterceptedExpressionInterpolator]
    public static Expression<Func<A, B, C, D, R>> Interpolate<R>(
        Expression<Func<IInterpolationContext, A, B, C, D, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Func<A, B, C, D, R>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [InterceptedExpressionInterpolator]
    public static Expression<Action<A, B, C, D>> Interpolate(
        Expression<Action<IInterpolationContext, A, B, C, D>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Action<A, B, C, D>>(default, expression);

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
    [InterceptedExpressionInterpolator]
    public static Expression<Action<A, B, C, D>> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A, B, C, D>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Action<A, B, C, D>>(data, expression);

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
    public static Expression<Func<A, B, C, D, R>> InterpolateRuntimeFallback<R>(
        Expression<Func<IInterpolationContext, A, B, C, D, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Func<A, B, C, D, R>>(default, expression);

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
    public static Expression<Action<A, B, C, D>> InterpolateRuntimeFallback(
        Expression<Action<IInterpolationContext, A, B, C, D>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Action<A, B, C, D>>(default, expression);

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
