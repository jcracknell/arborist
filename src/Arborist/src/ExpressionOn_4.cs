namespace Arborist;

public static partial class ExpressionOn<A, B, C, D> {
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
    /// Creates a constant-valued expression with the provided <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The result type of the constant-valued expression.
    /// </typeparam>
    public static Expression<Func<A, B, C, D, T>> Constant<T>(T value) =>
        ExpressionHelper.Const<Func<A, B, C, D, T>>(default, value);

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
}
