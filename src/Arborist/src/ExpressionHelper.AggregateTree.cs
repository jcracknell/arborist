using Arborist.Internal;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a balanced expression tree
    /// where the results of each individual expression are combined using the provided
    /// <paramref name="binaryOperator"/> expression.
    /// </summary>
    /// <param name="fallback">
    /// The fallback expression returned if the provided collection of
    /// <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary combining operator applied to the results of individual expressions and
    /// expression subtrees.
    /// </param>
    public static Expression<Func<R>> AggregateTree<R>(
        IEnumerable<Expression<Func<R>>> expressions,
        Expression<Func<R>> fallback,
        Expression<Func<R, R, R>> binaryOperator
    ) =>
        (Expression<Func<R>>)AggregateTreeImpl(expressions, fallback, binaryOperator);

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a balanced expression tree
    /// where the results of each individual expression are combined using the provided
    /// <paramref name="binaryOperator"/> expression.
    /// </summary>
    /// <param name="fallback">
    /// The fallback expression returned if the provided collection of
    /// <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary combining operator applied to the results of individual expressions and
    /// expression subtrees.
    /// </param>
    public static Expression<Func<A, R>> AggregateTree<A, R>(
        IEnumerable<Expression<Func<A, R>>> expressions,
        Expression<Func<A, R>> fallback,
        Expression<Func<R, R, R>> binaryOperator
    ) =>
        (Expression<Func<A, R>>)AggregateTreeImpl(expressions, fallback, binaryOperator);

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a balanced expression tree
    /// where the results of each individual expression are combined using the provided
    /// <paramref name="binaryOperator"/> expression.
    /// </summary>
    /// <param name="fallback">
    /// The fallback expression returned if the provided collection of
    /// <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary combining operator applied to the results of individual expressions and
    /// expression subtrees.
    /// </param>
    public static Expression<Func<A, B, R>> AggregateTree<A, B, R>(
        IEnumerable<Expression<Func<A, B, R>>> expressions,
        Expression<Func<A, B, R>> fallback,
        Expression<Func<R, R, R>> binaryOperator
    ) =>
        (Expression<Func<A, B, R>>)AggregateTreeImpl(expressions, fallback, binaryOperator);

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a balanced expression tree
    /// where the results of each individual expression are combined using the provided
    /// <paramref name="binaryOperator"/> expression.
    /// </summary>
    /// <param name="fallback">
    /// The fallback expression returned if the provided collection of
    /// <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary combining operator applied to the results of individual expressions and
    /// expression subtrees.
    /// </param>
    public static Expression<Func<A, B, C, R>> AggregateTree<A, B, C, R>(
        IEnumerable<Expression<Func<A, B, C, R>>> expressions,
        Expression<Func<A, B, C, R>> fallback,
        Expression<Func<R, R, R>> binaryOperator
    ) =>
        (Expression<Func<A, B, C, R>>)AggregateTreeImpl(expressions, fallback, binaryOperator);

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a balanced expression tree
    /// where the results of each individual expression are combined using the provided
    /// <paramref name="binaryOperator"/> expression.
    /// </summary>
    /// <param name="fallback">
    /// The fallback expression returned if the provided collection of
    /// <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary combining operator applied to the results of individual expressions and
    /// expression subtrees.
    /// </param>
    public static Expression<Func<A, B, C, D, R>> AggregateTree<A, B, C, D, R>(
        IEnumerable<Expression<Func<A, B, C, D, R>>> expressions,
        Expression<Func<A, B, C, D, R>> fallback,
        Expression<Func<R, R, R>> binaryOperator
    ) =>
        (Expression<Func<A, B, C, D, R>>)AggregateTreeImpl(expressions, fallback, binaryOperator);

    public static LambdaExpression AggregateTreeUnsafe(
        IEnumerable<LambdaExpression> expressions,
        LambdaExpression fallback,
        LambdaExpression binaryOperator
    ) {
        return AggregateTreeImpl(expressions, fallback, binaryOperator);
    }

    private static LambdaExpression AggregateTreeImpl(
        IEnumerable<LambdaExpression> expressions,
        LambdaExpression fallback,
        LambdaExpression binaryOperator
    ) {
        var expressionList = expressions as IReadOnlyList<LambdaExpression> ?? expressions.ToList();
        switch(expressionList.Count) {
            case 0: return fallback;
            case 1: return expressionList[0];
        }

        var replacements = new Dictionary<Expression, Expression>();
        var replacementVisitor = new ReplacingExpressionVisitor(replacements);

        return Expression.Lambda(
            AggregateTreeBody(expressionList, binaryOperator, 0, expressionList.Count, fallback.Parameters, replacements, replacementVisitor),
            fallback.Parameters
        );
    }

    internal static Expression AggregateTreeBody(
        IReadOnlyList<LambdaExpression> expressions,
        LambdaExpression binaryOperator,
        int start,
        int end,
        IReadOnlyCollection<ParameterExpression> parameters,
        Dictionary<Expression, Expression> replacements,
        ReplacingExpressionVisitor replacingVisitor
    ) {
        if(1 == end - start) {
            replacements.Clear();
            foreach(var (sp, rp) in expressions[start].Parameters.Zip(parameters))
                replacements[sp] = rp;

            return replacingVisitor.Visit(expressions[start].Body);
        }

        // Add any remainder to the midpoint to make the resulting expression left-biased
        var middle = start + Math.DivRem(end - start, 2, out var rem) + rem;

        var left = AggregateTreeBody(expressions, binaryOperator, start, middle, parameters, replacements, replacingVisitor);
        var right = AggregateTreeBody(expressions, binaryOperator, middle, end, parameters, replacements, replacingVisitor);

        replacements.Clear();
        replacements[binaryOperator.Parameters[0]] = left;
        replacements[binaryOperator.Parameters[1]] = right;

        return replacingVisitor.Visit(binaryOperator.Body);
    }
}
