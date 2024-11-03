using System.Collections;

namespace Arborist.Orderings;

internal sealed class OrderingNil<TSelector> : Ordering<TSelector> {
    public static OrderingNil<TSelector> Instance { get; } = new();

    private static OrderingEnumerator<TSelector> Enumerator { get; } = new(Instance);

    private OrderingNil() { }

    public OrderingTerm<TSelector> Term =>
        throw new InvalidOperationException(nameof(Term));

    IOrderingLike IOrderingLike.Rest => Rest;

    IOrderingTermLike IOrderingLike.Term => Term;

    public Ordering<TSelector> Rest =>
        throw new InvalidOperationException(nameof(Rest));

    public bool IsEmpty => true;

    public IEnumerator<OrderingTerm<TSelector>> GetEnumerator() =>
        Enumerator;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override int GetHashCode() =>
        HashCode.Combine(nameof(OrderingNil<TSelector>));

    public override bool Equals(object? obj) =>
        Equals(obj as IOrderingLike);

    public bool Equals(IOrderingLike? that) =>
        that is not null && that.IsEmpty;
}
