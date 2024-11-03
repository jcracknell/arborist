namespace Arborist.Orderings;

public static partial class OrderingExtensions {
    /// <summary>
    /// Adds a term specified by the provided <paramref name="selector"/> and <paramref name="direction"/> to
    /// the subject ordering, returning a new instance.
    /// </summary>
    public static Ordering<TSelector> ThenBy<TSelector>(
        this Ordering<TSelector> ordering,
        TSelector selector,
        OrderingDirection direction
    ) =>
        ordering.ThenBy(OrderingTerm.Create(selector, direction));

    /// <summary>
    /// Adds the provided <paramref name="term"/> to the subject ordering, returning a new instance.
    /// </summary>
    public static Ordering<TSelector> ThenBy<TSelector>(
        this Ordering<TSelector> ordering,
        OrderingTerm<TSelector> term
    ) {
        var builder = new OrderingBuilder<TSelector>();
        builder.AddRange(ordering);
        builder.Add(term);
        return builder.Build();
    }

    /// <summary>
    /// Adds the provided <paramref name="terms"/> to the subject ordering, returning a new instance.
    /// </summary>
    public static Ordering<TSelector> ThenBy<TSelector>(
        this Ordering<TSelector> ordering,
        Ordering<TSelector> terms
    ) {
        var builder = new OrderingBuilder<TSelector>();
        builder.AddRange(ordering);
        builder.AddRange(terms);
        return builder.Build();
    }

    /// <summary>
    /// Adds the provided <paramref name="terms"/> to the subject ordering, returning a new instance.
    /// </summary>
    public static Ordering<TSelector> ThenBy<TSelector>(
        this Ordering<TSelector> ordering,
        IEnumerable<OrderingTerm<TSelector>> terms
    ) {
        var builder = new OrderingBuilder<TSelector>();
        builder.AddRange(ordering);
        builder.AddRange(terms);
        return builder.Build();
    }

    /// <summary>
    /// Adds a term sorting the provided <paramref name="selector"/> in the <see cref="OrderingDirection.Ascending"/>
    /// direction to the subject ordering, returning a new instance.
    /// </summary>
    public static Ordering<TSelector> ThenByAscending<TSelector>(
        this Ordering<TSelector> ordering,
        TSelector selector
    ) =>
        ordering.ThenBy(selector, OrderingDirection.Ascending);

    /// <summary>
    /// Adds a term sorting the provided <paramref name="selector"/> in the <see cref="OrderingDirection.Descending"/>
    /// direction to the subject ordering, returning a new instance.
    /// </summary>
    public static Ordering<TSelector> ThenByDescending<TSelector>(
        this Ordering<TSelector> ordering,
        TSelector selector
    ) =>
        ordering.ThenBy(selector, OrderingDirection.Descending);
}
