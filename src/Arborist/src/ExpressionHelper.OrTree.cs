using Arborist.Internal;

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
    public static Expression<TPredicate> OrTree<TPredicate>(params Expression<TPredicate>[] predicates)
        where TPredicate : Delegate =>
        OrTree(predicates.AsEnumerable());

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
    public static Expression<TPredicate> OrTree<TPredicate>(IEnumerable<Expression<TPredicate>> predicates)
        where TPredicate : Delegate
    {
        AssertPredicateType(typeof(TPredicate));

        var predicateList = CollectionHelpers.AsReadOnlyList(predicates);

        return (Expression<TPredicate>)AggregateTreeImpl(
            expressions: predicateList,
            fallback: Const<TPredicate>(predicateList.FirstOrDefault()?.Parameters, false),
            binaryOperator: ExpressionOn<bool, bool>.Of(static (a, b) => a || b)
        );
    }
}
