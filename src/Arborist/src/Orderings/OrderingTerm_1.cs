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
}
