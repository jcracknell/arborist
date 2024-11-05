namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ORing their bodies together. Returns a false-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<bool>> OrTree(
        IEnumerable<Expression<Func<bool>>> predicates
    ) =>
        OrTreeUnsafe(predicates);

    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ORing their bodies together. Returns a false-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<A, bool>> OrTree<A>(
        IEnumerable<Expression<Func<A, bool>>> predicates
    ) =>
        OrTreeUnsafe(predicates);

    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ORing their bodies together. Returns a false-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<A, B, bool>> OrTree<A, B>(
        IEnumerable<Expression<Func<A, B, bool>>> predicates
    ) =>
        OrTreeUnsafe(predicates);

    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ORing their bodies together. Returns a false-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<A, B, C, bool>> OrTree<A, B, C>(
        IEnumerable<Expression<Func<A, B, C, bool>>> predicates
    ) =>
        OrTreeUnsafe(predicates);

    /// <summary>
    /// Combines the provided <paramref name="predicates"/> into a balanced expression tree by
    /// ORing their bodies together. Returns a false-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<Func<A, B, C, D, bool>> OrTree<A, B, C, D>(
        IEnumerable<Expression<Func<A, B, C, D, bool>>> predicates
    ) =>
        OrTreeUnsafe(predicates);

    public static Expression<TPredicate> OrTreeUnsafe<TPredicate>(IEnumerable<Expression<TPredicate>> predicates)
        where TPredicate : Delegate =>
        (Expression<TPredicate>)AggregateTreeImpl(
            expressions: predicates,
            fallback: Const<TPredicate>(false),
            binaryOperator: ExpressionOn<bool, bool>.Of(static (a, b) => a || b)
        );
}
