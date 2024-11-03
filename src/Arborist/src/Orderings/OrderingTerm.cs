namespace Arborist.Orderings;

/// <summary>
/// Static factory for <see cref="OrderingTerm{TSelector}"/> instances.
/// </summary>
public static class OrderingTerm {
    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// and <paramref name="direction"/>.
    /// </summary>
    public static OrderingTerm<TSelector> Create<TSelector>(TSelector selector, OrderingDirection direction) =>
        new OrderingTermImpl<TSelector>(selector, direction);

    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Ascending"/> direction.
    /// </summary>
    public static OrderingTerm<TSelector> Ascending<TSelector>(TSelector selector) =>
        Create(selector, OrderingDirection.Ascending);

    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Descending"/> direction.
    /// </summary>
    public static OrderingTerm<TSelector> Descending<TSelector>(TSelector selector) =>
        Create(selector, OrderingDirection.Descending);
}
