namespace Arborist.Orderings;

/// <summary>
/// <see cref="IComparer{T}"/> implementation wrapping an <see cref="Ordering{TSelector}"/> with
/// comparer selectors.
/// </summary>
public class OrderingComparer<A>(Ordering<IComparer<A>> ordering) : IComparer<A> {
    public int Compare(A? a, A? b) {
        if(a is null)
            return b is null ? 0 : 1;
        if(b is null)
            return -1;

        var rest = ordering;
        while(!rest.IsEmpty) {
            var term = rest.Term;
            var result = term.Selector.Compare(a, b);
            if(result != 0)
                return term.Direction switch {
                    OrderingDirection.Ascending => result,
                    OrderingDirection.Descending => -result,
                    _ => throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {term.Direction}.")
                };

            rest = rest.Rest;
        }

        return 0;
    }
}
