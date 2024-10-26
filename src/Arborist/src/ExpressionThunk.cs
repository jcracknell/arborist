using Arborist.Interpolation;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Arborist;

public static class ExpressionThunk {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<R>> Of<R>(Expression<Func<R>> expression) =>
        expression;

    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructor<R>(Expression<Func<R>> expression) =>
        ExpressionHelpers.GetConstructor(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethod<R>(Expression<Func<R>> expression) =>
        ExpressionHelpers.GetMethod(expression);

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<R>> Graft<A, R>(Expression<Func<A>> root, Expression<Func<A, R>> branch) =>
        Expression.Lambda<Func<R>>(
            body: ExpressionHelpers.Replace(branch.Body, branch.Parameters[0], root.Body),
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
    public static Expression<Func<R>> Interpolate<R>(
        Expression<Func<IInterpolationContext, R>> expression
    ) =>
        ExpressionHelpers.InterpolateCore<object?, Func<R>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [ExpressionInterpolator]
    public static Expression<Action> Interpolate(
        Expression<Action<IInterpolationContext>> expression
    ) =>
        ExpressionHelpers.InterpolateCore<object?, Action>(default, expression);

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
    public static Expression<Func<R>> Interpolate<TData, R>(
        TData data,
        Expression<Func<IInterpolationContext<TData>, R>> expression
    ) =>
        ExpressionHelpers.InterpolateCore<TData, Func<R>>(data, expression);

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
    public static Expression<Action> Interpolate<TData>(
        TData data,
        Expression<Action<IInterpolationContext<TData>>> expression
    ) =>
        ExpressionHelpers.InterpolateCore<TData, Action>(data, expression);

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructor<R>(
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelpers.TryGetConstructor(expression, out constructorInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethod<R>(
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelpers.TryGetMethod(expression, out methodInfo);
}
