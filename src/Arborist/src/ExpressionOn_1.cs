using System.Reflection;

namespace Arborist;

public static partial class ExpressionOn<A> {
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
    /// Creates a constant-valued expression with the provided <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The result type of the constant-valued expression.
    /// </typeparam>
    public static Expression<Func<A, T>> Constant<T>(T value) =>
        ExpressionHelper.Const<Func<A, T>>(default, value);

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
    /// The identity expression: <c>a => a</c>.
    /// </summary>
    public static Expression<Func<A, A>> Identity { get; } = Of(a => a);

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
