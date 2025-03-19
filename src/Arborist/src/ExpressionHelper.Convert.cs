using Arborist.Utils;

namespace Arborist;

public static partial class ExpressionHelper {
    internal static Expression<TDelegate> AsCore<TDelegate>(
        Type targetType,
        LambdaExpression expression
    )
        where TDelegate : Delegate
    {
        AssertFuncType(expression.Type);
        AssertParameterTypesCompatible(expression.Type, GetParameterTypes(typeof(TDelegate)));

        return Expression.Lambda<TDelegate>(
            Expression.TypeAs(expression.Body, targetType),
            expression.Parameters
        );
    }

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<T>> Convert<T, R>(
        TypeOf<T> type,
        Expression<Func<R>> expression
    ) =>
        ConvertImpl<Func<T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<A, T>> Convert<T, A, R>(
        TypeOf<T> type,
        Expression<Func<A, R>> expression
    ) =>
        ConvertImpl<Func<A, T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<A, B, T>> Convert<T, A, B, R>(
        TypeOf<T> type,
        Expression<Func<A, B, R>> expression
    ) =>
        ConvertImpl<Func<A, B, T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<A, B, C, T>> Convert<T, A, B, C, R>(
        TypeOf<T> type,
        Expression<Func<A, B, C, R>> expression
    ) =>
        ConvertImpl<Func<A, B, C, T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<A, B, C, D, T>> Convert<T, A, B, C, D, R>(
        TypeOf<T> type,
        Expression<Func<A, B, C, D, R>> expression
    ) =>
        ConvertImpl<Func<A, B, C, D, T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<T>> ConvertChecked<T, R>(
        TypeOf<T> type,
        Expression<Func<R>> expression
    ) =>
        ConvertCheckedImpl<Func<T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<A, T>> ConvertChecked<T, A, R>(
        TypeOf<T> type,
        Expression<Func<A, R>> expression
    ) =>
        ConvertCheckedImpl<Func<A, T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<A, B, T>> ConvertChecked<T, A, B, R>(
        TypeOf<T> type,
        Expression<Func<A, B, R>> expression
    ) =>
        ConvertCheckedImpl<Func<A, B, T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<A, B, C, T>> ConvertChecked<T, A, B, C, R>(
        TypeOf<T> type,
        Expression<Func<A, B, C, R>> expression
    ) =>
        ConvertCheckedImpl<Func<A, B, C, T>>(type.Type, expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No conversion operator is defined from the expression body type to <typeparamref name="T"/>.
    /// </exception>
    public static Expression<Func<A, B, C, D, T>> ConvertChecked<T, A, B, C, D, R>(
        TypeOf<T> type,
        Expression<Func<A, B, C, D, R>> expression
    ) =>
        ConvertCheckedImpl<Func<A, B, C, D, T>>(type.Type, expression);

    internal static Expression<TDelegate> ConvertImpl<TDelegate>(Type type, LambdaExpression expression)
        where TDelegate : Delegate =>
        Expression.Lambda<TDelegate>(
            Expression.Convert(expression.Body, type),
            expression.Parameters
        );

    internal static Expression<TDelegate> ConvertCheckedImpl<TDelegate>(Type type, LambdaExpression expression)
        where TDelegate : Delegate =>
        Expression.Lambda<TDelegate>(
            Expression.ConvertChecked(expression.Body, type),
            expression.Parameters
        );
}
