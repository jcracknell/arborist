namespace Arborist;

public static partial class ExpressionHelper {
    internal static Expression<TDelegate> GraftNullableImpl<TDelegate>(
        LambdaExpression root,
        LambdaExpression branch
    )
        where TDelegate : Delegate
    {
        AssertFuncType(typeof(TDelegate));

        var rootResultType = root.Type.GenericTypeArguments[^1];
        var branchInputType = branch.Type.GenericTypeArguments[0];

        // Calculate the result type of the expression. Note that this is based on the type of the
        // branch expression to avoid introducing explicit casts which would otherwise be handled
        // by the expression type!
        var resultType = (branch.Body.Type.IsValueType && !IsNullableType(branch.Body.Type)) switch {
            true => typeof(System.Nullable<>).MakeGenericType(branch.Body.Type),
            false => branch.Body.Type
        };

        return Expression.Lambda<TDelegate>(
            body: Expression.Condition(
                Expression.Equal(
                    root.Body,
                    Expression.Constant(null, root.Body.Type.IsValueType switch {
                        true => root.Body.Type,
                        false => typeof(object)
                    })
                ),
                Expression.Constant(null, resultType),
                // Coerce struct branch result to Nullable<T>
                Coerce(resultType, Replace(
                    branch.Body,
                    branch.Parameters[0],
                    // Coerce subtype to supertype or interface
                    Coerce(branchInputType, IsNullableType(rootResultType) switch {
                        true => Expression.Property(root.Body, rootResultType.GetProperty("Value")!),
                        false => root.Body
                    })
                ))
            ),
            parameters: root.Parameters
        );

        static bool IsNullableType(Type type) =>
            System.Nullable.GetUnderlyingType(type) is not null;

        static Expression Coerce(Type type, Expression expression) =>
            type == expression.Type ? expression : Expression.Convert(expression, type);
    }
}
