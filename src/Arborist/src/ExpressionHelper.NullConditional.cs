using Arborist.Utils;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Converts the provided <paramref name="expression"/> to an equivalent expression
    /// which is null-conditional on its input, producing the value of the provided
    /// <paramref name="expression"/> if the input is non-null.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type constraint.
    /// </param>
    public static Expression<Func<A?, B?>> NullConditional<A, B>(
        Expression<Func<A, B>> expression,
        Dummy<(A, B)> dummy = default
    )
        where A : class?
        where B : class? =>
        NullConditionalImpl<Func<A?, B?>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> to an equivalent expression
    /// which is null-conditional on its input, producing the value of the provided
    /// <paramref name="expression"/> if the input is non-null.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type constraint.
    /// </param>
    public static Expression<Func<A?, Nullable<B>>> NullConditional<A, B>(
        Expression<Func<A, B>> expression,
        Dummy<(A, Nullable<B>)> dummy = default
    )
        where A : class?
        where B : struct =>
        NullConditionalImpl<Func<A?, Nullable<B>>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> to an equivalent expression
    /// which is null-conditional on its input, producing the value of the provided
    /// <paramref name="expression"/> if the input is non-null.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type constraint.
    /// </param>
    public static Expression<Func<A?, Nullable<B>>> NullConditional<A, B>(
        Expression<Func<A, Nullable<B>>> expression,
        Dummy<(A, Nullable<B>)>? dummy = default
    )
        where A : class?
        where B : struct =>
        NullConditionalImpl<Func<A?, Nullable<B>>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> to an equivalent expression
    /// which is null-conditional on its input, producing the value of the provided
    /// <paramref name="expression"/> if the input is non-null.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type constraint.
    /// </param>
    public static Expression<Func<A?, B?>> NullConditional<A, B>(
        Expression<Func<A, B>> expression,
        Dummy<(Nullable<A>, B)> dummy = default
    )
        where A : struct
        where B : class? =>
        NullConditionalImpl<Func<A?, B?>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> to an equivalent expression
    /// which is null-conditional on its input, producing the value of the provided
    /// <paramref name="expression"/> if the input is non-null.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type constraint.
    /// </param>
    public static Expression<Func<A?, Nullable<B>>> NullConditional<A, B>(
        Expression<Func<A, B>> expression,
        Dummy<(Nullable<A>, Nullable<B>)> dummy = default
    )
        where A : struct
        where B : struct =>
        NullConditionalImpl<Func<A?, B?>>(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> to an equivalent expression
    /// which is null-conditional on its input, producing the value of the provided
    /// <paramref name="expression"/> if the input is non-null.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type constraint.
    /// </param>
    public static Expression<Func<A?, Nullable<B>>> NullConditional<A, B>(
        Expression<Func<A, Nullable<B>>> expression,
        Dummy<(Nullable<A>, Nullable<B>)> dummy = default
    )
        where A : struct
        where B : struct =>
        NullConditionalImpl<Func<A?, Nullable<B>>>(expression);

    internal static Expression<TDelegate> NullConditionalImpl<TDelegate>(
        LambdaExpression expression
    )
        where TDelegate : Delegate
    {
        AssertFuncType(typeof(TDelegate));

        var inputType = expression.Type.GenericTypeArguments[0];
        var nullableInputType = MkNullableType(inputType);
        // The result type is determined by the type of the body and not the declared expression type
        // to avoid introducing unnecessary explicit casts (e.g. to object)
        var resultType = MkNullableType(expression.Body.Type);

        var parameter = expression.Parameters[0];
        var nullableParameter = Expression.Parameter(nullableInputType, parameter.Name);

        return Expression.Lambda<TDelegate>(
            body: Expression.Condition(
                Expression.NotEqual(
                    nullableParameter,
                    Expression.Constant(null, inputType.IsValueType switch {
                        true => nullableInputType,
                        false => typeof(object)
                    })
                ),
                // Coerce struct branch result to Nullable<T>
                Coerce(resultType, Replace(
                    expression.Body,
                    parameter,
                    IsNullableType(nullableInputType) switch {
                        true => Expression.Property(nullableParameter, nullableInputType.GetProperty("Value")!),
                        false => nullableParameter
                    }
                )),
                Expression.Constant(null, resultType)
            ),
            parameters: new[] { nullableParameter }
        );

        static Type MkNullableType(Type type) =>
            (type.IsValueType && !IsNullableType(type)) switch {
                true => typeof(System.Nullable<>).MakeGenericType(type),
                false => type
            };

        static bool IsNullableType(Type type) =>
            System.Nullable.GetUnderlyingType(type) is not null;

        static Expression Coerce(Type type, Expression expression) =>
            type == expression.Type ? expression : Expression.Convert(expression, type);
    }
}
