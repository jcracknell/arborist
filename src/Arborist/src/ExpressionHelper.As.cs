using Arborist.Utils;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<T?>> As<R, T>(
        Expression<Func<R>> expression,
        TypeOf<T> type
    )
        where T : class =>
        AsImpl<Func<T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, T?>> As<A, R, T>(
        Expression<Func<A, R>> expression,
        TypeOf<T> type
    )
        where T : class =>
        AsImpl<Func<A, T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, B, T?>> As<A, B, R, T>(
        Expression<Func<A, B, R>> expression,
        TypeOf<T> type
    )
        where T : class =>
        AsImpl<Func<A, B, T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, B, C, T?>> As<A, B, C, R, T>(
        Expression<Func<A, B, C, R>> expression,
        TypeOf<T> type
    )
        where T : class =>
        AsImpl<Func<A, B, C, T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, B, C, D, T?>> As<A, B, C, D, R, T>(
        Expression<Func<A, B, C, D, R>> expression,
        TypeOf<T> type
    )
        where T : class =>
        AsImpl<Func<A, B, C, D, T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<T?>> As<R, T>(
        Expression<Func<R>> expression,
        TypeOf<Nullable<T>> type
    )
        where T : struct =>
        AsImpl<Func<T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, T?>> As<A, R, T>(
        Expression<Func<A, R>> expression,
        TypeOf<Nullable<T>> type
    )
        where T : struct =>
        AsImpl<Func<A, T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, B, T?>> As<A, B, R, T>(
        Expression<Func<A, B, R>> expression,
        TypeOf<Nullable<T>> type
    )
        where T : struct =>
        AsImpl<Func<A, B, T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, B, C, T?>> As<A, B, C, R, T>(
        Expression<Func<A, B, C, R>> expression,
        TypeOf<Nullable<T>> type
    )
        where T : struct =>
        AsImpl<Func<A, B, C, T?>>(expression, type.Type);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, B, C, D, T?>> As<A, B, C, D, R, T>(
        Expression<Func<A, B, C, D, R>> expression,
        TypeOf<Nullable<T>> type
    )
        where T : struct =>
        AsImpl<Func<A, B, C, D, T?>>(expression, type.Type);

    internal static Expression<TDelegate> AsImpl<TDelegate>(LambdaExpression expression, Type type)
        where TDelegate : Delegate =>
        Expression.Lambda<TDelegate>(
            Expression.TypeAs(expression.Body, type),
            expression.Parameters
        );
}
