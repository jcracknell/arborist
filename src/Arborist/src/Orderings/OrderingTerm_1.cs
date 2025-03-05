using Arborist.Orderings.JsonConverters;
using System.Text.Json.Serialization;

namespace Arborist.Orderings;

/// <summary>
/// A term appearing in an <see cref="Ordering{TSelector}"/> composed of a selector of
/// type <typeparamref name="TSelector"/>, and an <see cref="OrderingDirection"/>.
/// </summary>
/// <seealso cref="OrderingTerm"/>
/// <seealso cref="Ordering{TSelector}"/>
[JsonConverter(typeof(OrderingTermJsonConverterFactory))]
public interface OrderingTerm<out TSelector> : IOrderingTermLike {
    public new TSelector Selector { get; }

    object? IOrderingTermLike.Selector => Selector;

    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// and <paramref name="direction"/>.
    /// </summary>
    public static OrderingTerm<TSelector> Create(TSelector selector, OrderingDirection direction) =>
        new OrderingTermImpl<TSelector>(selector, direction);

    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Ascending"/> direction.
    /// </summary>
    public static OrderingTerm<TSelector> Ascending(TSelector selector) =>
        Create(selector, OrderingDirection.Ascending);

    /// <summary>
    /// Constructs an <see cref="OrderingTerm{TSelector}"/> from the provided <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Descending"/> direction.
    /// </summary>
    public static OrderingTerm<TSelector> Descending(TSelector selector) =>
        Create(selector, OrderingDirection.Descending);
}
