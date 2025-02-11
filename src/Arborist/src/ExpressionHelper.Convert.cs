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

    internal static Expression<TDelegate> ConvertCore<TDelegate>(
        Type targetType,
        LambdaExpression expression
    )
        where TDelegate : Delegate
    {
        AssertFuncType(expression.Type);
        AssertParameterTypesCompatible(expression.Type, GetParameterTypes(typeof(TDelegate)));

        return Expression.Lambda<TDelegate>(
            Expression.Convert(expression.Body, targetType),
            expression.Parameters
        );
    }

    internal static Expression<TDelegate> ConvertCheckedCore<TDelegate>(
        Type targetType,
        LambdaExpression expression
    )
        where TDelegate : Delegate
    {
        AssertFuncType(expression.Type);
        AssertParameterTypesCompatible(expression.Type, GetParameterTypes(typeof(TDelegate)));

        return Expression.Lambda<TDelegate>(
            Expression.ConvertChecked(expression.Body, targetType),
            expression.Parameters
        );
    }
}
