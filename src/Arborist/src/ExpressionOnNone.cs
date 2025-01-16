using Arborist.Interpolation;
using Arborist.Interpolation.Internal;
using System.Reflection;

namespace Arborist;

public static class ExpressionOnNone {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<R>> Of<R>(Expression<Func<R>> expression) =>
        expression;

    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action> Of(Expression<Action> expression) =>
        expression;

    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructorInfo<R>(
        Expression<Func<R>> expression
    ) =>
        ExpressionHelper.GetConstructorInfo(expression);


    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo<R>(
        Expression<Func<R>> expression
    ) =>
        ExpressionHelper.GetMethodInfo(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo(
        Expression<Action> expression
    ) =>
        ExpressionHelper.GetMethodInfo(expression);

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<RR>> Graft<R, RR>(
        Expression<Func<R>> root,
        Expression<Func<R, RR>> branch
    ) =>
        Expression.Lambda<Func<RR>>(
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
    [CompileTimeExpressionInterpolator]
    public static Expression<Func<R>> Interpolate<R>(
        Expression<Func<IInterpolationContext, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Func<R>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [CompileTimeExpressionInterpolator]
    public static Expression<Action> Interpolate(
        Expression<Action<IInterpolationContext>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Action>(default, expression);

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
    public static Expression<Func<R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Func<R>>(data, expression);

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
    public static Expression<Action> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Action>(data, expression);

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
    public static Expression<Func<R>> InterpolateRuntimeFallback<R>(
        Expression<Func<IInterpolationContext, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Func<R>>(default, expression);

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
    public static Expression<Action> InterpolateRuntimeFallback(
        Expression<Action<IInterpolationContext>> expression
    ) =>
        ExpressionHelper.InterpolateCore<object?, Action>(default, expression);

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
    public static Expression<Func<R>> InterpolateRuntimeFallback<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, R>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Func<R>>(data, expression);

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
    public static Expression<Action> InterpolateRuntimeFallback<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>>> expression
    ) =>
        ExpressionHelper.InterpolateCore<TData, Action>(data, expression);

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructorInfo<R>(
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelper.TryGetConstructorInfo(expression, out constructorInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo<R>(
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethodInfo(expression, out methodInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo(
        Expression<Action> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethodInfo(expression, out methodInfo);

}
