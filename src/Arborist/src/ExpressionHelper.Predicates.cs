using Arborist.Utils;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Creates a negated version of the provided predicate <paramref name="expression"/>.
    /// </summary>
    public static Expression<TPredicate> Not<TPredicate>(Expression<TPredicate> expression) {
        AssertPredicateType(typeof(TPredicate));

        return Expression.Lambda<TPredicate>(
            Expression.Not(expression.Body),
            expression.Parameters
        );
    }

    /// <summary>
    /// Creates a predicate expression which is true if the input is not null and the provided
    /// <paramref name="predicate"/> holds.
    /// </summary>
    public static Expression<Func<A?, bool>> NotNullAnd<A>(Expression<Func<A, bool>> predicate, Dummy<A> dummy = default)
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
    public static Expression<Func<A?, bool>> NotNullAnd<A>(Expression<Func<A, bool>> predicate, Dummy<Nullable<A>> dummy = default)
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
    public static Expression<Func<A?, bool>> NullOr<A>(Expression<Func<A, bool>> predicate, Dummy<A> dummy = default)
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
    public static Expression<Func<A?, bool>> NullOr<A>(Expression<Func<A, bool>> predicate, Dummy<Nullable<A>> dummy = default)
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
