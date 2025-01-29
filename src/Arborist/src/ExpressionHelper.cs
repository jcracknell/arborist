namespace Arborist;

public static partial class ExpressionHelper {
    internal static void AssertDelegateType(Type type) {
        if(!IsDelegateType(type))
            throw new InvalidOperationException($"Invalid delegate type: {type}.");
    }

    internal static void AssertActionType(Type type) {
        if(!IsActionType(type))
            throw new InvalidOperationException($"Invalid Action type: {type}.");
    }

    internal static void AssertFuncType(Type type) {
        if(!IsFuncType(type))
            throw new InvalidOperationException($"Invalid Func type: {type}.");
    }

    internal static void AssertPredicateType(Type type) {
        if(!IsPredicateType(type))
            throw new InvalidOperationException($"Invalid predicate type: {type}.");
    }
    
    internal static void AssertParameterTypesCompatible(Type delegateType, ReadOnlySpan<Type> parameterTypes) {
        if(!AreParameterTypesCompatible(delegateType, parameterTypes))
            throw new InvalidOperationException($"Delegate type {delegateType} is not compatible with the expected parameter types.");
    }

    internal static bool IsDelegateType(Type type) =>
        IsFuncType(type) || IsActionType(type);

    internal static bool IsActionType(Type type) =>
        type.IsAssignableTo(typeof(Delegate)) && (
            type.FullName?.StartsWith("System.Action`") is true
            || type.FullName?.Equals("System.Action") is true
        );
        
    internal static bool IsFuncType(Type type) =>
        type.IsAssignableTo(typeof(Delegate))
        && type.FullName?.StartsWith("System.Func`") is true;

    internal static bool IsPredicateType(Type type) =>
        IsFuncType(type) && typeof(bool) == type.GetGenericArguments()[^1];
        
    internal static bool AreParameterTypesCompatible(
        Type delegateType,
        ReadOnlySpan<Type> expectedParameterTypes
    ) {
        var delegateParameterTypes = GetParameterTypes(delegateType);
        if(expectedParameterTypes.Length != delegateParameterTypes.Length)
            return false;
            
        for(var i = 0; i < expectedParameterTypes.Length; i++)
            if(!expectedParameterTypes[i].IsAssignableTo(delegateParameterTypes[i]))
                return false;
                
        return true;
    }
    
    private static ReadOnlySpan<Type> GetParameterTypes(Type delegateType) {
        AssertDelegateType(delegateType);
        
        if(!delegateType.IsGenericType)
            return ReadOnlySpan<Type>.Empty;
            
        var typeArguments = delegateType.GetGenericArguments();
        if(IsActionType(delegateType))
            return typeArguments;
            
        return typeArguments.AsSpan(0, typeArguments.Length - 1);
    }

    internal static Expression<TFunc> Const<TFunc>(IReadOnlyCollection<ParameterExpression>? parameters, object? value)
        where TFunc : Delegate
    {
        AssertFuncType(typeof(TFunc));

        var genericArguments = typeof(TFunc).GetGenericArguments();
        var resultType = genericArguments[^1];

        return Expression.Lambda<TFunc>(
            Expression.Constant(value, resultType),
            parameters ?? genericArguments[..^1].Select(Expression.Parameter)
        );
    }
}
