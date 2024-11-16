using Arborist.Internal;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<bool>> And(
        IEnumerable<Expression<Func<bool>>> predicates
    ) =>
        AndUnsafe(predicates);

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<A, bool>> And<A>(
        IEnumerable<Expression<Func<A, bool>>> predicates
    ) =>
        AndUnsafe(predicates);

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<A, B, bool>> And<A, B>(
        IEnumerable<Expression<Func<A, B, bool>>> predicates
    ) =>
        AndUnsafe(predicates);

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<A, B, C, bool>> And<A, B, C>(
        IEnumerable<Expression<Func<A, B, C, bool>>> predicates
    ) =>
        AndUnsafe(predicates);

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<Func<A, B, C, D, bool>> And<A, B, C, D>(
        IEnumerable<Expression<Func<A, B, C, D, bool>>> predicates
    ) =>
        AndUnsafe(predicates);

    public static Expression<TPredicate> AndUnsafe<TPredicate>(IEnumerable<Expression<TPredicate>> predicates)
        where TPredicate : Delegate
    {
        var predicateList = CollectionHelpers.AsReadOnlyList(predicates);

        return (Expression<TPredicate>)AggregateImpl(
            expressions: predicateList,
            seed: Const<TPredicate>(predicateList.FirstOrDefault()?.Parameters, true),
            binaryOperator: ExpressionOn<bool, bool>.Of(static (a, b) => a && b),
            options: new() { DiscardSeedExpression = true }
        );
    }
}
