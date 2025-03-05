namespace Arborist.Orderings;

internal struct OrderingBuilder<TSelector> {
    private Ordering<TSelector>? _ordering;
    private OrderingCons<TSelector>? _cons;
    private bool _referenced;

    public OrderingBuilder(Ordering<TSelector> ordering) {
        AddRangeOrdering(ordering);
    }

    public Ordering<TSelector> Build() {
        if(_ordering is null)
            return OrderingNil<TSelector>.Instance;

        _referenced = true;
        return _ordering;
    }

    private void CopyReferenced() {
        if(!_referenced || _ordering is null)
            return;

        var cons = new OrderingCons<TSelector>(_ordering.Term, OrderingNil<TSelector>.Instance);
        var rest = _ordering.Rest;
        _ordering = _cons = cons;
        _referenced = false;

        while(!rest.IsEmpty) {
            cons = new OrderingCons<TSelector>(rest.Term, OrderingNil<TSelector>.Instance);
            _cons.Rest = cons;
            _cons = cons;
            rest = rest.Rest;
        }
    }

    /// <summary>
    /// Attempts to copy the provided ordering into the builder as the referenced builder value.
    /// Returns false if there is already an ordering in the builder, the ordering is empty, or
    /// the ordering cannot be copied.
    /// </summary>
    private bool TryInstallOrdering(Ordering<TSelector> ordering) {
        if(_ordering is not null)
            return false;
        if(ordering.IsEmpty)
            return false;

        // Note that we can set cons to null, as it will be populated by CopyReferenced by the next Add
        _ordering = ordering;
        _cons = default;
        _referenced = true;

        return true;
    }

    public void Add(OrderingTerm<TSelector> term) {
        CopyReferenced();

        var cons = new OrderingCons<TSelector>(term, OrderingNil<TSelector>.Instance);
        if(_ordering is null) {
            _ordering = _cons = cons;
        } else {
            _cons!.Rest = cons;
            _cons = cons;
        }
    }

    public void AddRange(OrderingTerm<TSelector>[] terms) {
        AddRange(terms.AsSpan());
    }

    public void AddRange(ReadOnlySpan<OrderingTerm<TSelector>> terms) {
        for(var i = 0; i < terms.Length; i++)
            Add(terms[i]);
    }

    public void AddRange(IEnumerable<OrderingTerm<TSelector>> terms) {
        switch(terms) {
            case IReadOnlyCollection<OrderingTerm<TSelector>> { Count: 0 }: return;
            case Ordering<TSelector> ordering:
                AddRangeOrdering(ordering);
                return;
            default:
                AddRangeEnumerated(terms);
                return;
        }
    }

    private void AddRangeOrdering(Ordering<TSelector> terms) {
        if(terms.IsEmpty)
            return;
        if(TryInstallOrdering(terms))
            return;

        var rest = terms;
        do {
            Add(rest.Term);
            rest = rest.Rest;
        } while(!rest.IsEmpty);
    }

    private void AddRangeEnumerated(IEnumerable<OrderingTerm<TSelector>> terms) {
        using var enumerator = terms.GetEnumerator();

        if(!enumerator.MoveNext())
            return;

        do {
            Add(enumerator.Current);
        } while(enumerator.MoveNext());
    }
}
