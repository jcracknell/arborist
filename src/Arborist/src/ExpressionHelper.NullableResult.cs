using Arborist.Utils;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Converts the provided <paramref name="expression"/> so that its declared result type is nullable.
    /// </summary>
    public static Expression<Func<R?>> NullableResult<R>(
        Expression<Func<R>> expression
    )
        where R : class =>
        NullableResultRef<Func<R?>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> so that its declared result type is nullable.
    /// </summary>
    public static Expression<Func<A, R?>> NullableResult<A, R>(
        Expression<Func<A, R>> expression
    )
        where R : class =>
        NullableResultRef<Func<A, R?>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> so that its declared result type is nullable.
    /// </summary>
    public static Expression<Func<A, B, R?>> NullableResult<A, B, R>(
        Expression<Func<A, B, R>> expression
    )
        where R : class =>
        NullableResultRef<Func<A, B, R?>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> so that its declared result type is nullable.
    /// </summary>
    public static Expression<Func<A, B, C, R?>> NullableResult<A, B, C, R>(
        Expression<Func<A, B, C, R>> expression
    )
        where R : class =>
        NullableResultRef<Func<A, B, C, R?>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> so that its declared result type is nullable.
    /// </summary>
    public static Expression<Func<A, B, C, D, R?>> NullableResult<A, B, C, D, R>(
        Expression<Func<A, B, C, D, R>> expression
    )
        where R : class =>
        NullableResultRef<Func<A, B, C, D, R?>>(expression);


    /// <summary>
    /// Wraps the provided <paramref name="expression"/> to produce a <see cref="System.Nullable{T}"/>
    /// result value.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<Nullable<R>>> NullableResult<R>(
        Expression<Func<R>> expression,
        Dummy dummy = default
    )
        where R : struct =>
        NullableResultStruct<R, Func<Nullable<R>>>(expression);

    /// <summary>
    /// Wraps the provided <paramref name="expression"/> to produce a <see cref="System.Nullable{T}"/>
    /// result value.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, Nullable<R>>> NullableResult<A, R>(
        Expression<Func<A, R>> expression,
        Dummy dummy = default
    )
        where R : struct =>
        NullableResultStruct<R, Func<A, Nullable<R>>>(expression);

    /// <summary>
    /// Wraps the provided <paramref name="expression"/> to produce a <see cref="System.Nullable{T}"/>
    /// result value.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, Nullable<R>>> NullableResult<A, B, R>(
        Expression<Func<A, B, R>> expression,
        Dummy dummy = default
    )
        where R : struct =>
        NullableResultStruct<R, Func<A, B, Nullable<R>>>(expression);

    /// <summary>
    /// Wraps the provided <paramref name="expression"/> to produce a <see cref="System.Nullable{T}"/>
    /// result value.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, C, Nullable<R>>> NullableResult<A, B, C, R>(
        Expression<Func<A, B, C, R>> expression,
        Dummy dummy = default
    )
        where R : struct =>
        NullableResultStruct<R, Func<A, B, C, Nullable<R>>>(expression);

    /// <summary>
    /// Wraps the provided <paramref name="expression"/> to produce a <see cref="System.Nullable{T}"/>
    /// result value.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, C, D, Nullable<R>>> NullableResult<A, B, C, D, R>(
        Expression<Func<A, B, C, D, R>> expression,
        Dummy dummy = default
    )
        where R : struct =>
        NullableResultStruct<R, Func<A, B, C, D, Nullable<R>>>(expression);

    private static Expression<TDelegate> NullableResultRef<TDelegate>(LambdaExpression expression) =>
        // This cast is legal because the nullable and non-nullable delegate types are the same at runtime
        (Expression<TDelegate>)expression;

    private static Expression<TDelegate> NullableResultStruct<R, TDelegate>(LambdaExpression expression)
        where R : struct
        where TDelegate : Delegate =>
        Expression.Lambda<TDelegate>(
            Expression.Convert(expression.Body, typeof(Nullable<R>)),
            expression.Parameters
        );
}
