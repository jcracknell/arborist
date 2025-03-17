using Arborist.Internal;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<TPredicate> And<TPredicate>(params Expression<TPredicate>[] predicates)
        where TPredicate : Delegate =>
        And(predicates.AsEnumerable());

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<TPredicate> And<TPredicate>(IEnumerable<Expression<TPredicate>> predicates)
        where TPredicate : Delegate
    {
        AssertPredicateType(typeof(TPredicate));

        var predicateList = CollectionHelpers.AsReadOnlyList(predicates);

        return (Expression<TPredicate>)AggregateImpl(
            expressions: predicateList,
            seed: Const<TPredicate>(predicateList.FirstOrDefault()?.Parameters, true),
            binaryOperator: ExpressionOn<bool, bool>.Of(static (a, b) => a && b),
            options: new() { DiscardSeedExpression = true }
        );
    }
}
