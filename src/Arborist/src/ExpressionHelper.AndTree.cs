namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ANDing their bodies together. Returns a true-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<bool>> AndTree(
        IEnumerable<Expression<Func<bool>>> predicates
    ) =>
        AndTreeUnsafe(predicates);

    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ANDing their bodies together. Returns a true-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<A, bool>> AndTree<A>(
        IEnumerable<Expression<Func<A, bool>>> predicates
    ) =>
        AndTreeUnsafe(predicates);

    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ANDing their bodies together. Returns a true-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<A, B, bool>> AndTree<A, B>(
        IEnumerable<Expression<Func<A, B, bool>>> predicates
    ) =>
        AndTreeUnsafe(predicates);

    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ANDing their bodies together. Returns a true-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<A, B, C, bool>> AndTree<A, B, C>(
        IEnumerable<Expression<Func<A, B, C, bool>>> predicates
    ) =>
        AndTreeUnsafe(predicates);

    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ANDing their bodies together. Returns a true-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<A, B, C, D, bool>> AndTree<A, B, C, D>(
        IEnumerable<Expression<Func<A, B, C, D, bool>>> predicates
    ) =>
        AndTreeUnsafe(predicates);

    public static Expression<TPredicate> AndTreeUnsafe<TPredicate>(IEnumerable<Expression<TPredicate>> predicates)
        where TPredicate : Delegate =>
        (Expression<TPredicate>)AggregateTreeImpl(
            expressions: predicates,
            fallback: Const<TPredicate>(true),
            binaryOperator: ExpressionOn<bool, bool>.Of(static (a, b) => a && b)
        );
}
