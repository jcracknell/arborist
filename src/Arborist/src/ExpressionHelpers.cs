namespace Arborist;

public static partial class ExpressionHelpers {
    private static void AssertActionExpressionType(Type type) {
        if(!IsPredicateExpressionType(type))
            throw new InvalidOperationException($"Invalid Action type: {type}.");
    }

    private static void AssertFuncExpressionType(Type type) {
        if(!IsFuncExpressionType(type))
            throw new InvalidOperationException($"Invalid Func type: {type}.");
    }

    private static void AssertPredicateExpressionType(Type type) {
        if(!IsPredicateExpressionType(type))
            throw new InvalidOperationException($"Invalid predicate type: {type}.");
    }

    private static bool IsActionExpressionType(Type type) =>
        type.IsAssignableTo(typeof(Delegate)) && (
            type.FullName?.StartsWith("System.Action`") is true
            || type.FullName?.Equals("System.Action") is true
        );

    private static bool IsFuncExpressionType(Type type) =>
        type.IsAssignableTo(typeof(Delegate))
        && type.FullName?.StartsWith("System.Func`") is true;

    private static bool IsPredicateExpressionType(Type type) =>
        IsFuncExpressionType(type) && typeof(bool) == type.GetGenericArguments()[^1];

    private static Expression<TDelegate> ChainedBinOp<TDelegate>(
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

    private static Expression<TDelegate> ChainedBinOpTree<TDelegate>(
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
        ) => (end - start) switch {
            1 => start switch {
                0 => expressionList[0].Body,
                _ => Replace(expressionList[start].Body, replacements),
            },
            var len => Expression.MakeBinary(
                binaryType: expressionType,
                left: Recurse(expressionType, expressionList, start, start + len / 2, replacements),
                right: Recurse(expressionType, expressionList, start + len / 2, end, replacements)
            )
        };
    }

    private static Expression<TDelegate> Const<TDelegate>(object? value)
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
