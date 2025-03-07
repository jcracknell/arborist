using System.Diagnostics.Contracts;

namespace Arborist.Orderings;

public static partial class OrderingExtensions {
    /// <summary>
    /// Applies the provided <paramref name="ordering"/> to the subject <see cref="IQueryable{T}"/>,
    /// overriding any existing ordering of results.
    /// </summary>
    [Pure]
    public static IQueryable<A> OrderBy<A, B>(
        this IQueryable<A> queryable,
        Ordering<Expression<Func<A, B>>> ordering
    ) =>
        ordering.IsEmpty switch {
            true => queryable,
            false => ordering.Term.Direction switch {
                OrderingDirection.Ascending => queryable.OrderBy(ordering.Term.Selector).ThenBy(ordering.Rest),
                OrderingDirection.Descending => queryable.OrderByDescending(ordering.Term.Selector).ThenBy(ordering.Rest),
                _ => throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {ordering.Term.Direction}.")
            }
        };

    /// <summary>
    /// Applies the provided <paramref name="ordering"/> to the subject <see cref="IQueryable{T}"/>
    /// as an extension to the currently defined ordering of results.
    /// </summary>
    [Pure]
    public static IOrderedQueryable<A> ThenBy<A, B>(
        this IOrderedQueryable<A> queryable,
        Ordering<Expression<Func<A, B>>> ordering
    ) =>
        ordering.IsEmpty switch {
            true => queryable,
            false => ordering.Term.Direction switch {
                OrderingDirection.Ascending => queryable.ThenBy(ordering.Term.Selector).ThenBy(ordering.Rest),
                OrderingDirection.Descending => queryable.ThenByDescending(ordering.Term.Selector).ThenBy(ordering.Rest),
                _ => throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {ordering.Term.Direction}.")
            }
        };
}
