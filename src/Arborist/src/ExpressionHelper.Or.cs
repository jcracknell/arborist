namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<bool>> Or(
        IEnumerable<Expression<Func<bool>>> predicates
    ) =>
        OrUnsafe(predicates);

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<A, bool>> Or<A>(
        IEnumerable<Expression<Func<A, bool>>> predicates
    ) =>
        OrUnsafe(predicates);

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<A, B, bool>> Or<A, B>(
        IEnumerable<Expression<Func<A, B, bool>>> predicates
    ) =>
        OrUnsafe(predicates);

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<A, B, C, bool>> Or<A, B, C>(
        IEnumerable<Expression<Func<A, B, C, bool>>> predicates
    ) =>
        OrUnsafe(predicates);

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<A, B, C, D, bool>> Or<A, B, C, D>(
        IEnumerable<Expression<Func<A, B, C, D, bool>>> predicates
    ) =>
        OrUnsafe(predicates);

    public static Expression<TPredicate> OrUnsafe<TPredicate>(IEnumerable<Expression<TPredicate>> predicates)
        where TPredicate : Delegate =>
        (Expression<TPredicate>)AggregateImpl(
            expressions: predicates,
            seed: Const<TPredicate>(false),
            binaryOperator: ExpressionOn<bool, bool>.Of(static (a, b) => a || b),
            options: new() { DiscardSeedExpression = false }
        );
}
