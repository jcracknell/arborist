using System.Collections;

namespace Arborist.Orderings;

internal sealed class OrderingCons<TSelector> : Ordering<TSelector> {
    public OrderingCons(OrderingTerm<TSelector> term, Ordering<TSelector> rest) {
        Term = term;
        Rest = rest;
    }

    public OrderingTerm<TSelector> Term { get; }
    public Ordering<TSelector> Rest { get; internal set; }
    public bool IsEmpty => false;

    public IEnumerator<OrderingTerm<TSelector>> GetEnumerator() =>
        new OrderingEnumerator<TSelector>(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IOrderingTermLike IOrderingLike.Term => Term;
    IOrderingLike IOrderingLike.Rest => Rest;

    public override int GetHashCode() {
        var hashCode = new HashCode();
        var rest = (Ordering<TSelector>)this;
        do {
            hashCode.Add(rest.Term);
            rest = rest.Rest;
        } while(!rest.IsEmpty);

        return hashCode.ToHashCode();
    }

    public override bool Equals(object? obj) =>
        Equals(obj as IOrderingLike);

    public bool Equals(IOrderingLike? that) {
        if(that is null)
            return false;
        if(ReferenceEquals(this, that))
            return true;

        return !that.IsEmpty
        && EqualityComparer<object>.Default.Equals(this.Term, that.Term)
        && this.Rest.Equals(that.Rest);
    }
}
