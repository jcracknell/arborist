namespace Arborist.Orderings;

public static partial class OrderingExtensions {
    /// <summary>
    /// Applies the provided <paramref name="ordering"/> to the subject <see cref="IEnumerable{T}"/>,
    /// overriding any existing ordering of results.
    /// </summary>
    public static IEnumerable<A> OrderBy<A, B>(
        this IEnumerable<A> enumerable,
        Ordering<Func<A, B>> ordering
    ) =>
        ordering.IsEmpty switch {
            true => enumerable,
            false => ordering.Term.Direction switch {
                OrderingDirection.Ascending => enumerable.OrderBy(ordering.Term.Selector).ThenBy(ordering.Rest),
                OrderingDirection.Descending => enumerable.OrderByDescending(ordering.Term.Selector).ThenBy(ordering.Rest),
                _ => throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {ordering.Term.Direction}.")
            }
        };

    /// <summary>
    /// Applies the provided <paramref name="ordering"/> to the subject <see cref="IEnumerable{T}"/>
    /// as an extension to the currently defined ordering of results.
    /// </summary>
    public static IOrderedEnumerable<A> ThenBy<A, B>(
        this IOrderedEnumerable<A> enumerable,
        Ordering<Func<A, B>> ordering
    ) =>
        ordering.IsEmpty switch {
            true => enumerable,
            false => ordering.Term.Direction switch {
                OrderingDirection.Ascending => enumerable.ThenBy(ordering.Term.Selector).ThenBy(ordering.Rest),
                OrderingDirection.Descending => enumerable.ThenByDescending(ordering.Term.Selector).ThenBy(ordering.Rest),
                _ => throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {ordering.Term.Direction}.")
            }
        };
    
}
