using System.Collections;

namespace Arborist.Orderings;

internal sealed class OrderingEnumerator<TSelector> : IEnumerator<OrderingTerm<TSelector>> {
    private readonly Ordering<TSelector> _ordering;
    private bool _moved;
    private Ordering<TSelector> _current;

    public OrderingEnumerator(Ordering<TSelector> ordering) {
        _ordering = ordering;
        _current = ordering;
        _moved = false;
    }

    public bool MoveNext() {
        if(!_moved)
            return _moved = !_current.IsEmpty;
        if(_current.Rest.IsEmpty)
            return false;

        _current = _current.Rest;
        return true;
    }

    public void Reset() {
        _current = _ordering;
        _moved = false;
    }

    public OrderingTerm<TSelector> Current => _current.Term;

    object? IEnumerator.Current => Current;

    public void Dispose() { }
}
