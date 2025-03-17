using Arborist.Internal;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<TPredicate> Or<TPredicate>(params Expression<TPredicate>[] predicates)
        where TPredicate : Delegate =>
        Or(predicates.AsEnumerable());

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<TPredicate> Or<TPredicate>(IEnumerable<Expression<TPredicate>> predicates)
        where TPredicate : Delegate
    {
        AssertPredicateType(typeof(TPredicate));

        var predicateList = CollectionHelpers.AsReadOnlyList(predicates);

        return (Expression<TPredicate>)AggregateImpl(
            expressions: predicateList,
            seed: Const<TPredicate>(predicateList.FirstOrDefault()?.Parameters, false),
            binaryOperator: ExpressionOn<bool, bool>.Of(static (a, b) => a || b),
            options: new() { DiscardSeedExpression = true }
        );
    }
}
