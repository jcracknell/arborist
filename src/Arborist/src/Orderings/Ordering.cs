namespace Arborist.Orderings;

/// <summary>
/// Static factory for <see cref="Ordering{TSelector}"/> instances.
/// </summary>
/// <seealso cref="Ordering{TSelector}"/>
public static class Ordering {
    /// <summary>
    /// Applies the provided <paramref name="direction"/> to the subject <see cref="Ordering{TSelector}"/>,
    /// returning the inverse ordering if the <paramref name="direction"/> is <see cref="OrderingDirection.Descending"/>.
    /// </summary>
    /// <remarks>
    /// This method is useful to conditionally invert "default" <see cref="Ordering{TSelector}"/>
    /// instances or translations of <typeparamref name="TSelector"/> values on the assumption that
    /// <see cref="OrderingDirection.Ascending"/> is the default <see cref="OrderingDirection"/>.
    /// </remarks>
    /// <seealso cref="Invert{TSelector}"/>
    public static Ordering<TSelector> ApplyDirection<TSelector>(
        this Ordering<TSelector> ordering,
        OrderingDirection direction
    ) =>
        OrderingDirection.Descending == direction ? ordering.Invert() : ordering;

    /// <summary>
    /// Creates a single-term <see cref="Ordering{TSelector}"/> using the specified <paramref name="selector"/>
    /// and <paramref name="direction"/>.
    /// </summary>
    public static Ordering<TSelector> By<TSelector>(TSelector selector, OrderingDirection direction) =>
        Ordering<TSelector>.By(selector, direction);

    /// <summary>
    /// Creates an <see cref="Ordering{TSelector}"/> containing the specified <paramref name="term"/>.
    /// </summary>
    public static Ordering<TSelector> By<TSelector>(OrderingTerm<TSelector> term) =>
        Ordering<TSelector>.By(term);

    /// <summary>
    /// Creates an <see cref="Ordering{TSelector}"/> using the specified <paramref name="terms"/>.
    /// </summary>
    public static Ordering<TSelector> By<TSelector>(IEnumerable<OrderingTerm<TSelector>> terms) =>
        Ordering<TSelector>.By(terms);

    /// <summary>
    /// Creates an <see cref="Ordering{TSelector}"/> using the specified <paramref name="terms"/>.
    /// </summary>
    public static Ordering<TSelector> By<TSelector>(params OrderingTerm<TSelector>[] terms) =>
        Ordering<TSelector>.By(terms);

    /// <summary>
    /// Creates an <see cref="Ordering{TSelector}"/> using the specified <paramref name="terms"/>.
    /// </summary>
    public static Ordering<TSelector> By<TSelector>(ReadOnlySpan<OrderingTerm<TSelector>> terms) =>
        Ordering<TSelector>.By(terms);

    /// <summary>
    /// Creates a single term <see cref="Ordering{TSelector}"/> using the specified <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Ascending"/> direction.
    /// </summary>
    public static Ordering<TSelector> ByAscending<TSelector>(TSelector selector) =>
        Ordering<TSelector>.ByAscending(selector);

    /// <summary>
    /// Creates a single term <see cref="Ordering{TSelector}"/> using the specified <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Descending"/> direction.
    /// </summary>
    public static Ordering<TSelector> ByDescending<TSelector>(TSelector selector) =>
        Ordering<TSelector>.ByDescending(selector);

    /// <summary>
    /// Inverts the direction of the subject <paramref name="ordering"/>, returning an ordering where the
    /// terms have the opposite <see cref="OrderingDirection"/>.
    /// </summary>
    public static Ordering<TSelector> Invert<TSelector>(this Ordering<TSelector> ordering) =>
        ordering.Select(OrderingTerm.Invert);
}
