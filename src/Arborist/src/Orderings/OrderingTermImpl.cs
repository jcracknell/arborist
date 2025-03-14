using Arborist.Orderings.JsonConverters;
using System.Text.Json.Serialization;

namespace Arborist.Orderings;

[JsonConverter(typeof(OrderingTermJsonConverterFactory))]
internal sealed class OrderingTermImpl<TSelector> : OrderingTerm<TSelector> {
    public OrderingTermImpl(TSelector selector, OrderingDirection direction) {
        Selector = selector;
        Direction = direction;
    }

    public TSelector Selector { get; }
    public OrderingDirection Direction { get; }

    public override int GetHashCode() =>
        HashCode.Combine(Selector, Direction);

    public override bool Equals(object? obj) =>
        Equals(obj as IOrderingTermLike);

    public bool Equals(IOrderingTermLike? that) {
        if(that is null)
            return false;
        if(!this.Direction.Equals(that.Direction))
            return false;

        return EqualityComparer<object>.Default.Equals(this.Selector, that.Selector);
    }
}
