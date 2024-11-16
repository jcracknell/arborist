using Arborist.Internal;

namespace Arborist;

public static partial class ExpressionHelper {
    public readonly struct AggregateOptions {
        /// <summary>
        /// When <c>true</c>, permits discarding the seed expression from the result provided that the
        /// the input expression collection is not empty, and the return type of the binary combining
        /// operator matches that of the input expressions.
        /// </summary>
        public bool DiscardSeedExpression { get; init; }
    }

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a single expression through chained
    /// left-associative application of the provided <paramref name="binaryOperator"/> to the results
    /// of the expressions, starting from the provided <paramref name="seed"/> expression.
    /// </summary>
    /// <param name="seed">
    /// The initial input result value provided to the <paramref name="binaryOperator"/>. Used as the
    /// default result in the event that the collection of <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary expression used to combine the results of previously processed expressions with
    /// the value of the next input expression.
    /// </param>
    public static Expression<Func<S>> Aggregate<R, S>(
        IEnumerable<Expression<Func<R>>> expressions,
        Expression<Func<S>> seed,
        Expression<Func<S, R, S>> binaryOperator,
        AggregateOptions options = default
    ) =>
        (Expression<Func<S>>)AggregateImpl(expressions, seed, binaryOperator, options);

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a single expression through chained
    /// left-associative application of the provided <paramref name="binaryOperator"/> to the results
    /// of the expressions, starting from the provided <paramref name="seed"/> expression.
    /// </summary>
    /// <param name="seed">
    /// The initial input result value provided to the <paramref name="binaryOperator"/>. Used as the
    /// default result in the event that the collection of <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary expression used to combine the results of previously processed expressions with
    /// the value of the next input expression.
    /// </param>
    public static Expression<Func<A, S>> Aggregate<A, R, S>(
        IEnumerable<Expression<Func<A, R>>> expressions,
        Expression<Func<A, S>> seed,
        Expression<Func<S, R, S>> binaryOperator,
        AggregateOptions options = default
    ) =>
        (Expression<Func<A, S>>)AggregateImpl(expressions, seed, binaryOperator, options);

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a single expression through chained
    /// left-associative application of the provided <paramref name="binaryOperator"/> to the results
    /// of the expressions, starting from the provided <paramref name="seed"/> expression.
    /// </summary>
    /// <param name="seed">
    /// The initial input result value provided to the <paramref name="binaryOperator"/>. Used as the
    /// default result in the event that the collection of <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary expression used to combine the results of previously processed expressions with
    /// the value of the next input expression.
    /// </param>
    public static Expression<Func<A, B, S>> Aggregate<A, B, R, S>(
        IEnumerable<Expression<Func<A, B, R>>> expressions,
        Expression<Func<A, B, S>> seed,
        Expression<Func<S, R, S>> binaryOperator,
        AggregateOptions options = default
    ) =>
        (Expression<Func<A, B, S>>)AggregateImpl(expressions, seed, binaryOperator, options);

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a single expression through chained
    /// left-associative application of the provided <paramref name="binaryOperator"/> to the results
    /// of the expressions, starting from the provided <paramref name="seed"/> expression.
    /// </summary>
    /// <param name="seed">
    /// The initial input result value provided to the <paramref name="binaryOperator"/>. Used as the
    /// default result in the event that the collection of <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary expression used to combine the results of previously processed expressions with
    /// the value of the next input expression.
    /// </param>
    public static Expression<Func<A, B, C, S>> Aggregate<A, B, C, R, S>(
        IEnumerable<Expression<Func<A, B, C, R>>> expressions,
        Expression<Func<A, B, C, S>> seed,
        Expression<Func<S, R, S>> binaryOperator,
        AggregateOptions options = default
    ) =>
        (Expression<Func<A, B, C, S>>)AggregateImpl(expressions, seed, binaryOperator, options);

    /// <summary>
    /// Combines the provided <paramref name="expressions"/> into a single expression through chained
    /// left-associative application of the provided <paramref name="binaryOperator"/> to the results
    /// of the expressions, starting from the provided <paramref name="seed"/> expression.
    /// </summary>
    /// <param name="seed">
    /// The initial input result value provided to the <paramref name="binaryOperator"/>. Used as the
    /// default result in the event that the collection of <paramref name="expressions"/> is empty.
    /// </param>
    /// <param name="binaryOperator">
    /// The binary expression used to combine the results of previously processed expressions with
    /// the value of the next input expression.
    /// </param>
    public static Expression<Func<A, B, C, D, S>> Aggregate<A, B, C, D, R, S>(
        IEnumerable<Expression<Func<A, B, C, D, R>>> expressions,
        Expression<Func<A, B, C, D, S>> seed,
        Expression<Func<S, R, S>> binaryOperator,
        AggregateOptions options = default
    ) =>
        (Expression<Func<A, B, C, D, S>>)AggregateImpl(expressions, seed, binaryOperator, options);

    public static LambdaExpression AggregateUnsafe(
        IEnumerable<LambdaExpression> expressions,
        LambdaExpression seed,
        LambdaExpression binaryOperator,
        AggregateOptions options = default
    ) {
        return AggregateImpl(expressions, seed, binaryOperator, options);
    }

    private static LambdaExpression AggregateImpl(
        IEnumerable<LambdaExpression> expressions,
        LambdaExpression seed,
        LambdaExpression binaryOperator,
        AggregateOptions options
    ) {
        using var enumerator = expressions.GetEnumerator();

        if(!enumerator.MoveNext())
            return seed;

        var replacements = new Dictionary<Expression, Expression>();
        var replacingVisitor = new ReplacingExpressionVisitor(replacements);
        var body = seed.Body;
        var op0 = binaryOperator.Parameters[0];
        var op1 = binaryOperator.Parameters[1];

        do {
            var current = enumerator.Current;

            replacements.Clear();
            foreach(var (ps, pr) in current.Parameters.Zip(seed.Parameters))
                replacements[ps] = pr;

            var right = replacingVisitor.Visit(current.Body);

            // If the return type of the first expression matches the return type of the binary operator,
            // then we do not need to use the seed expression as the basis for the resulting tree.
            if(options.DiscardSeedExpression && ReferenceEquals(body, seed.Body) && seed.ReturnType == current.ReturnType) {
                body = right;
                continue;
            }

            replacements.Clear();
            replacements[op0] = body;
            replacements[op1] = right;

            body = replacingVisitor.Visit(binaryOperator.Body);
        } while(enumerator.MoveNext());

        return Expression.Lambda(body, seed.Parameters);
    }
}

