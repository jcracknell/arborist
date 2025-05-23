using System.Diagnostics.Contracts;

namespace Arborist.Orderings;

/// <summary>
/// Static factory for <see cref="OrderingTerm{TSelector}"/> instances.
/// </summary>
public static class OrderingTerm {
    /// <summary>
    /// Applies the provided <paramref name="direction"/> to the subject <see cref="OrderingTerm{TSelector}"/>,
    /// returning the inversed term if the <paramref name="direction"/> is <see cref="OrderingDirection.Descending"/>.
    /// </summary>
    /// <seealso cref="Invert{TSelector}"/>
    [Pure]
    public static OrderingTerm<TSelector> ApplyDirection<TSelector>(
        this OrderingTerm<TSelector> term,
        OrderingDirection direction
    ) =>
        OrderingDirection.Descending == direction ? term.Invert() : term;

    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// and <paramref name="direction"/>.
    /// </summary>
    public static OrderingTerm<TSelector> Create<TSelector>(TSelector selector, OrderingDirection direction) =>
        OrderingTerm<TSelector>.Create(selector, direction);

    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Ascending"/> direction.
    /// </summary>
    public static OrderingTerm<TSelector> Ascending<TSelector>(TSelector selector) =>
        OrderingTerm<TSelector>.Ascending(selector);

    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Descending"/> direction.
    /// </summary>
    public static OrderingTerm<TSelector> Descending<TSelector>(TSelector selector) =>
        OrderingTerm<TSelector>.Descending(selector);

    /// <summary>
    /// Inverts the direction of the subject <see cref="OrderingTerm{TSelector}"/>, returning a term with the
    /// same selector and the opposite <see cref="OrderingDirection"/>.
    /// </summary>
    [Pure]
    public static OrderingTerm<TSelector> Invert<TSelector>(this OrderingTerm<TSelector> term) =>
        Create(term.Selector, term.Direction.Invert());
}
