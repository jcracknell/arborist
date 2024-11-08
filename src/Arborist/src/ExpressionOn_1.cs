using Arborist.Interpolation;
using System.Reflection;

namespace Arborist;

public static class ExpressionOn<A> {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, R>> Of<R>(Expression<Func<A, R>> expression) =>
        expression;

    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A>> Of(Expression<Action<A>> expression) =>
        expression;

    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructorInfo<R>(Expression<Func<A, R>> expression) =>
        ExpressionHelper.GetConstructorInfo(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo<R>(Expression<Func<A, R>> expression) =>
        ExpressionHelper.GetMethodInfo(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo(Expression<Action<A>> expression) =>
        ExpressionHelper.GetMethodInfo(expression);

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, RR>> Graft<R, RR>(
        Expression<Func<A, R>> root,
        Expression<Func<R, RR>> branch
    ) =>
        Expression.Lambda<Func<A, RR>>(
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
    [ExpressionInterpolator]
    public static Expression<Func<A, R>> Interpolate<R>(
        Expression<Func<IInterpolationContext, A, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Func<A, R>>(default(object), expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [ExpressionInterpolator]
    public static Expression<Action<A>> Interpolate(
        Expression<Action<IInterpolationContext, A>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Action<A>>(default(object), expression);

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
    public static Expression<Func<A, R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Func<A, R>>(data, expression);

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
    public static Expression<Action<A>> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>, A>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Action<A>>(data, expression);

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructorInfo<R>(
        Expression<Func<A, R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelper.TryGetConstructorInfo(expression, out constructorInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo<R>(
        Expression<Func<A, R>> expression,
        [MaybeNullWhen(false)] out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethodInfo(expression, out methodInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo(
        Expression<Action<A>> expression,
        [MaybeNullWhen(false)] out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethodInfo(expression, out methodInfo);
}
