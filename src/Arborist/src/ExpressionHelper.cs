namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Helpers for expressions accepting no parameters.
    /// </summary>
    public static IExpressionHelperOnNone OnNone =>
        ExpressionHelperOnNone.Instance;

    /// <summary>
    /// Helpers for expressions accepting a single parameter.
    /// </summary>
    public static IExpressionHelperOn<A> On<A>() =>
        ExpressionHelperOn<A>.Instance;
        
    /// <summary>
    /// Helpers for expressions accepting two parameters.
    /// </summary>
    public static IExpressionHelperOn<A, B> On<A, B>() =>
        ExpressionHelperOn<A, B>.Instance;
        
    /// <summary>
    /// Helpers for expressions accepting 3 parameters.
    /// </summary>
    public static IExpressionHelperOn<A, B, C> On<A, B, C>() =>
        ExpressionHelperOn<A, B, C>.Instance;
        
    /// <summary>
    /// Helpers for expressions accepting 4 parameters.
    /// </summary>
    public static IExpressionHelperOn<A, B, C, D> On<A, B, C, D>() =>
        ExpressionHelperOn<A, B, C, D>.Instance;
        
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

    internal static Expression<TDelegate> ChainedBinOp<TDelegate>(
        ExpressionType expressionType,
        object? zero,
        IEnumerable<Expression<TDelegate>> expressions
    ) where TDelegate : Delegate {
        if(expressions is not IReadOnlyList<Expression<TDelegate>> expressionList)
            return ChainedBinOp(expressionType, zero, expressions.ToList());

        AssertFuncExpressionType(typeof(TDelegate));

        switch(expressionList.Count) {
            case 0: return Const<TDelegate>(zero);
            case 1: return expressionList[0];
        }

        var head = expressionList[0];

        return Expression.Lambda<TDelegate>(
            body: expressionList.Skip(1).Aggregate(head.Body, (acc, expr) => Expression.MakeBinary(
                binaryType: expressionType,
                left: acc,
                right: Replace(expr.Body, expr.Parameters.Zip(head.Parameters).ToDictionary(
                    tup => (Expression)tup.First,
                    tup => (Expression)tup.Second
                ))
            )),
            parameters: head.Parameters
        );
    }

    internal static Expression<TDelegate> ChainedBinOpTree<TDelegate>(
        ExpressionType expressionType,
        object? zero,
        IEnumerable<Expression<TDelegate>> expressions
    ) where TDelegate : Delegate {
        if(expressions is not IReadOnlyList<Expression<TDelegate>> expressionList)
            return ChainedBinOpTree(expressionType, zero, expressions.ToList());

        AssertFuncExpressionType(typeof(TDelegate));

        switch(expressionList.Count) {
            case 0: return Const<TDelegate>(zero);
            case 1: return expressionList[0];
        }

        var head = expressionList[0];
        var parameterCount = expressionList.Count * head.Parameters.Count;
        var replacements = new Dictionary<Expression, Expression>(parameterCount);
        foreach(var expr in expressionList.Skip(1))
            foreach(var (search, replace) in head.Parameters.Zip(expr.Parameters))
                replacements[search] = replace;

        return Expression.Lambda<TDelegate>(
            Recurse(expressionType, expressionList, 0, expressionList.Count, replacements),
            expressionList[0].Parameters
        );

        static Expression Recurse(
            ExpressionType expressionType,
            IReadOnlyList<Expression<TDelegate>> expressionList,
            int start,
            int end,
            IReadOnlyDictionary<Expression, Expression> replacements
        ) {
            if(1 == end - start) {
                // As a minor optimization, we can return the body of the initial expression directly as we
                // are using its parameters in the result expression.
                if(0 == start)
                    return expressionList[0].Body;

                return Replace(expressionList[start].Body, replacements);
            }

            // Add any remainder to the midpoint to make the resulting expression left-biased
            var middle = start + Math.DivRem(end - start, 2, out var rem) + rem;

            return Expression.MakeBinary(
                binaryType: expressionType,
                left: Recurse(expressionType, expressionList, start, middle, replacements),
                right: Recurse(expressionType, expressionList, middle, end, replacements)
            );
        }
    }

    internal static Expression<TDelegate> Const<TDelegate>(object? value)
        where TDelegate : Delegate
    {
        AssertFuncExpressionType(typeof(TDelegate));

        var genericArguments = typeof(TDelegate).GetGenericArguments();
        var resultType = genericArguments[^1];

        return Expression.Lambda<TDelegate>(
            Expression.Constant(value, resultType),
            genericArguments[..^1].Select(Expression.Parameter)
        );
    }
}
