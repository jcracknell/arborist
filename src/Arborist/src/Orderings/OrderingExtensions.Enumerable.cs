using Arborist.Internal;
using System.Diagnostics.Contracts;

namespace Arborist.Orderings;

public static partial class OrderingExtensions {
    /// <summary>
    /// Applies the provided <paramref name="ordering"/> over <see cref="IComparer{T}"/> instances to the subject
    /// <see cref="IEnumerable{T}"/>, overriding any previously applied ordering of elements.
    /// </summary>
    [Pure]
    public static IOrderedEnumerable<A> OrderBy<A>(this IEnumerable<A> enumerable, Ordering<IComparer<A>> ordering) =>
        enumerable.OrderBy(FunctionHelpers.Identity, new OrderingComparer<A>(ordering));

    /// <summary>
    /// Applies the provided <paramref name="ordering"/> to the subject <see cref="IEnumerable{T}"/>,
    /// overriding any previously applied ordering of elements.
    /// </summary>
    [Pure]
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
    /// Applies the provided <paramref name="ordering"/> over <see cref="IComparer{T}"/> instances to the subject
    /// <see cref="IEnumerable{T}"/>, preserving any previously applied ordering of elements.
    /// </summary>
    [Pure]
    public static IOrderedEnumerable<A> ThenBy<A>(this IOrderedEnumerable<A> enumerable, Ordering<IComparer<A>> ordering) =>
        enumerable.ThenBy(FunctionHelpers.Identity, new OrderingComparer<A>(ordering));

    /// <summary>
    /// Applies the provided <paramref name="ordering"/> to the subject <see cref="IEnumerable{T}"/>,
    /// preserving any previously applied ordering of elements.
    /// </summary>
    [Pure]
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
