using Arborist.Utils;

namespace Arborist;

public static partial class ExpressionHelper {
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

        return ChainedBinOp(ExpressionType.AndAlso, true, expressions);
    }

    /// <summary>
    /// Combines the provided predicate <paramref name="expressions"/> into a balanced expression tree by
    /// ANDing their bodies together. Returns a true-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth as compared to the
    /// results of <see cref="And{TDelegate}(IEnumerable{Expression{TDelegate}})"/>, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<TDelegate> AndTree<TDelegate>(params Expression<TDelegate>[] expressions)
        where TDelegate : Delegate =>
        AndTree(expressions.AsEnumerable());

    /// <summary>
    /// Combines the provided predicate <paramref name="expressions"/> into a balanced expression tree by
    /// ANDing their bodies together. Returns a true-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth as compared to the
    /// results of <see cref="And{TDelegate}(IEnumerable{Expression{TDelegate}})"/>, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<TDelegate> AndTree<TDelegate>(IEnumerable<Expression<TDelegate>> expressions)
        where TDelegate : Delegate
    {
        AssertPredicateExpressionType(typeof(TDelegate));

        return ChainedBinOpTree(ExpressionType.AndAlso, true, expressions);
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

        return ChainedBinOp(ExpressionType.OrElse, false, expressions);
    }

    /// <summary>
    /// Combines the provided predicate <paramref name="expressions"/> into a balanced expression tree by
    /// ORing their bodies together. Returns a false-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth as compared to the
    /// results of <see cref="Or{TDelegate}(IEnumerable{Expression{TDelegate}})"/>, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<TDelegate> OrTree<TDelegate>(params Expression<TDelegate>[] expressions)
        where TDelegate : Delegate =>
        OrTree(expressions.AsEnumerable());

    /// <summary>
    /// Combines the provided predicate <paramref name="expressions"/> into a balanced expression tree by
    /// ORing their bodies together. Returns a false-valued predicate expression if the provided collection
    /// of predicates is empty.
    /// </summary>
    /// <remarks>
    /// This method is useful as it significantly reduces the maximum expression depth as compared to the
    /// results of <see cref="Or{TDelegate}(IEnumerable{Expression{TDelegate}})"/>, which is important
    /// in certain scenarios (i.e. when interpreted by EntityFramework to produce queries for Microsoft
    /// SQL Server).
    /// </remarks>
    public static Expression<TDelegate> OrTree<TDelegate>(IEnumerable<Expression<TDelegate>> expressions)
        where TDelegate : Delegate
    {
        AssertPredicateExpressionType(typeof(TDelegate));

        return ChainedBinOpTree(ExpressionType.OrElse, false, expressions);
    }

    /// <summary>
    /// Creates a negated version of the provided predicate <paramref name="expression"/>.
    /// </summary>
    public static Expression<TDelegate> Not<TDelegate>(Expression<TDelegate> expression) {
        AssertPredicateExpressionType(typeof(TDelegate));

        return Expression.Lambda<TDelegate>(
            Expression.Not(expression.Body),
            expression.Parameters
        );
    }

    /// <summary>
    /// Creates a predicate expression which is true if the input is not null and the provided
    /// <paramref name="predicate"/> holds.
    /// </summary>
    public static Expression<Func<A?, bool>> NotNullAnd<A>(Expression<Func<A, bool>> predicate, DummyClass? dummy = default)
        where A : class
    {
        return Expression.Lambda<Func<A?, bool>>(
            Expression.AndAlso(
                Expression.NotEqual(
                    predicate.Parameters[0],
                    Expression.Constant(default(A), typeof(A))
                ),
                predicate.Body
            ),
            predicate.Parameters
        );
    }

    /// <summary>
    /// Creates a predicate expression which is true if the input is not null and the provided
    /// <paramref name="predicate"/> holds.
    /// </summary>
    public static Expression<Func<A?, bool>> NotNullAnd<A>(Expression<Func<A, bool>> predicate, DummyStruct? dummy = default)
        where A : struct
    {
        var parameter = Expression.Parameter(typeof(A?), predicate.Parameters[0].Name);

        return Expression.Lambda<Func<A?, bool>>(
            Expression.AndAlso(
                Expression.Property(parameter, typeof(A?).GetProperty("HasValue")!),
                Replace(
                    predicate.Body,
                    predicate.Parameters[0],
                    Expression.Property(parameter, typeof(A?).GetProperty("Value")!)
                )
            ),
            parameter
        );
    }

    /// <summary>
    /// Creates a predicate expression which is true if the input is null or the provided
    /// <paramref name="predicate"/> holds.
    /// </summary>
    public static Expression<Func<A?, bool>> NullOr<A>(Expression<Func<A, bool>> predicate, DummyClass? dummy = default)
        where A : class
    {
        return Expression.Lambda<Func<A?, bool>>(
            Expression.OrElse(
                Expression.Equal(
                    predicate.Parameters[0],
                    Expression.Constant(default(A), typeof(A))
                ),
                predicate.Body
            ),
            predicate.Parameters
        );
    }

    /// <summary>
    /// Creates a predicate expression which is true if the input is null or the provided
    /// <paramref name="predicate"/> holds.
    /// </summary>
    public static Expression<Func<A?, bool>> NullOr<A>(Expression<Func<A, bool>> predicate, DummyStruct dummy = default)
        where A : struct
    {
        var parameter = Expression.Parameter(typeof(A?), predicate.Parameters[0].Name);

        return Expression.Lambda<Func<A?, bool>>(
            Expression.OrElse(
                Expression.Not(Expression.Property(parameter, typeof(A?).GetProperty("HasValue")!)),
                Replace(
                    predicate.Body,
                    predicate.Parameters[0],
                    Expression.Property(parameter, typeof(A?).GetProperty("Value")!)
                )
            ),
            parameter
        );
    }
}
