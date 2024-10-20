namespace Arborist;

public static partial class ExpressionHelpers {
    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<TDelegate> And<TDelegate>(params Expression<TDelegate>[] expressions)
        where TDelegate : Delegate =>
        And(expressions.AsEnumerable());

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ANDing their bodies
    /// together. Returns a true-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<TDelegate> And<TDelegate>(IEnumerable<Expression<TDelegate>> expressions)
        where TDelegate : Delegate
    {
        AssertPredicateExpressionType(typeof(TDelegate));

        return ChainedBinOp(ExpressionType.AndAlso, Const<TDelegate>(true), expressions);
    }

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<TDelegate> Or<TDelegate>(params Expression<TDelegate>[] expressions)
        where TDelegate : Delegate =>
        Or(expressions.AsEnumerable());

    /// <summary>
    /// Combines the provided predicate expressions into a single expression by ORing their bodies
    /// together. Returns a false-valued predicate expression if the provided collection of predicates
    /// is empty.
    /// </summary>
    public static Expression<TDelegate> Or<TDelegate>(IEnumerable<Expression<TDelegate>> expressions)
        where TDelegate : Delegate
    {
        AssertPredicateExpressionType(typeof(TDelegate));

        return ChainedBinOp(ExpressionType.OrElse, Const<TDelegate>(false), expressions);
    }
}
