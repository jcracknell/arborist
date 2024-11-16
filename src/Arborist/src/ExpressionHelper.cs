namespace Arborist;

public static partial class ExpressionHelper {
    internal static void AssertActionExpressionType(Type type) {
        if(!IsActionExpressionType(type))
            throw new InvalidOperationException($"Invalid Action type: {type}.");
    }

    internal static void AssertFuncExpressionType(Type type) {
        if(!IsFuncExpressionType(type))
            throw new InvalidOperationException($"Invalid Func type: {type}.");
    }

    internal static void AssertPredicateExpressionType(Type type) {
        if(!IsPredicateExpressionType(type))
            throw new InvalidOperationException($"Invalid predicate type: {type}.");
    }

    internal static bool IsActionExpressionType(Type type) =>
        type.IsAssignableTo(typeof(Delegate)) && (
            type.FullName?.StartsWith("System.Action`") is true
            || type.FullName?.Equals("System.Action") is true
        );

    internal static bool IsFuncExpressionType(Type type) =>
        type.IsAssignableTo(typeof(Delegate))
        && type.FullName?.StartsWith("System.Func`") is true;

    internal static bool IsPredicateExpressionType(Type type) =>
        IsFuncExpressionType(type) && typeof(bool) == type.GetGenericArguments()[^1];

    internal static Expression<TFunc> Const<TFunc>(IReadOnlyCollection<ParameterExpression>? parameters, object? value)
        where TFunc : Delegate
    {
        AssertFuncExpressionType(typeof(TFunc));

        var genericArguments = typeof(TFunc).GetGenericArguments();
        var resultType = genericArguments[^1];

        return Expression.Lambda<TFunc>(
            Expression.Constant(value, resultType),
            parameters ?? genericArguments[..^1].Select(Expression.Parameter)
        );
    }
}
