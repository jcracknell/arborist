using System.Diagnostics.Contracts;

namespace Arborist.Orderings;

public static partial class OrderingExtensions {
    /// <summary>
    /// Simplifies the subject ordering, omitting terms with previously observed selectors and
    /// dropping all terms following a term defining an absolute ordering.
    /// </summary>
    /// <seealso cref="IOrderingSelector{TSelf}"/>
    /// <seealso cref="IOrderingSelectorComparer{TSelector}"/>
    [Pure]
    public static Ordering<TSelector> Simplify<TSelector>(this Ordering<TSelector> ordering) =>
        ordering.Simplify(OrderingSelectorComparer<TSelector>.Default);

    /// <summary>
    /// Simplifies the subject ordering, omitting terms with previously observed selectors and
    /// dropping all terms following a term defining an absolute ordering.
    /// </summary>
    /// <seealso cref="IOrderingSelector{TSelf}"/>
    /// <seealso cref="IOrderingSelectorComparer{TSelector}"/>
    [Pure]
    public static Ordering<TSelector> Simplify<TSelector>(
        this Ordering<TSelector> ordering,
        IEqualityComparer<TSelector> equalityComparer
    ) =>
        ordering.Simplify(new DefaultOrderingSelectorComparer<TSelector>(equalityComparer));

    /// <summary>
    /// Simplifies the subject ordering, omitting terms with previously observed selectors and
    /// dropping all terms following a term defining an absolute ordering.
    /// </summary>
    [Pure]
    public static Ordering<TSelector> Simplify<TSelector>(
        this Ordering<TSelector> ordering,
        IOrderingSelectorComparer<TSelector> selectorComparer
    ) {
        if(ordering.IsEmpty)
            return ordering;

        var observed = default(HashSet<TSelector>);
        var builder = new OrderingBuilder<TSelector>();
        var rest = ordering;
        do {
            var selector = rest.Term.Selector;

            // Drop previously observed selectors
            if(observed?.Contains(selector) is not true) {
                builder.Add(rest.Term);

                // Drop all terms following a term whose selector represents an absolute ordering
                if(selector is not null && selectorComparer.IsAbsoluteOrdering(selector))
                    break;
            }

            (observed ??= new(selectorComparer)).Add(selector);
            rest = rest.Rest;
        } while(!rest.IsEmpty);

        return builder.Build();
    }

    /// <summary>
    /// Adds a term specified by the provided <paramref name="selector"/> and <paramref name="direction"/> to
    /// the subject ordering, returning a new instance.
    /// </summary>
    [Pure]
    public static Ordering<TSelector> ThenBy<TSelector>(
        this Ordering<TSelector> ordering,
        TSelector selector,
        OrderingDirection direction
    ) =>
        ordering.ThenBy(OrderingTerm.Create(selector, direction));

    /// <summary>
    /// Adds the provided <paramref name="term"/> to the subject ordering, returning a new instance.
    /// </summary>
    [Pure]
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
    [Pure]
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
    /// Adds the provided <paramref name="terms"/> to the subject ordering, returning a new instance.
    /// </summary>
    [Pure]
    public static Ordering<TSelector> ThenBy<TSelector>(
        this Ordering<TSelector> ordering,
        params OrderingTerm<TSelector>[] terms
    ) =>
        ordering.ThenBy(terms.AsSpan());

    /// <summary>
    /// Adds the provided <paramref name="terms"/> to the subject ordering, returning a new instance.
    /// </summary>
    [Pure]
    public static Ordering<TSelector> ThenBy<TSelector>(
        this Ordering<TSelector> ordering,
        ReadOnlySpan<OrderingTerm<TSelector>> terms
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
    [Pure]
    public static Ordering<TSelector> ThenByAscending<TSelector>(
        this Ordering<TSelector> ordering,
        TSelector selector
    ) =>
        ordering.ThenBy(selector, OrderingDirection.Ascending);

    /// <summary>
    /// Adds a term sorting the provided <paramref name="selector"/> in the <see cref="OrderingDirection.Descending"/>
    /// direction to the subject ordering, returning a new instance.
    /// </summary>
    [Pure]
    public static Ordering<TSelector> ThenByDescending<TSelector>(
        this Ordering<TSelector> ordering,
        TSelector selector
    ) =>
        ordering.ThenBy(selector, OrderingDirection.Descending);
}
