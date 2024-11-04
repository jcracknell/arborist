namespace Arborist.Orderings;

internal struct OrderingBuilder<TSelector> {
    private Ordering<TSelector>? _ordering;
    private OrderingCons<TSelector>? _cons;
    private bool _referenced;

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
