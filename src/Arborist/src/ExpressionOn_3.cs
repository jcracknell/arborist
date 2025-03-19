namespace Arborist;

public static partial class ExpressionOn<A, B, C> {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, B, C, R>> Of<R>(Expression<Func<A, B, C, R>> expression) =>
        expression;

    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A, B, C>> Of(Expression<Action<A, B, C>> expression) =>
        expression;

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, B, C, T>> As<T>(LambdaExpression expression) =>
        ExpressionHelper.AsCore<Func<A, B, C, T>>(typeof(T), expression);

    /// <summary>
    /// Creates a constant-valued expression with the provided <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The result type of the constant-valued expression.
    /// </typeparam>
    public static Expression<Func<A, B, C, T>> Constant<T>(T value) =>
        ExpressionHelper.Const<Func<A, B, C, T>>(default, value);
}
