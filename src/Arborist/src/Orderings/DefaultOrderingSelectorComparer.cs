namespace Arborist.Orderings;

/// <summary>
/// Default implementation of <see cref="IOrderingSelectorComparer{TSelector}"/> based on a provided
/// <see cref="IEqualityComparer{T}"/>.
/// </summary>
internal sealed class DefaultOrderingSelectorComparer<TSelector>(
    IEqualityComparer<TSelector> equalityComparer
)
    : IOrderingSelectorComparer<TSelector>
{
    public int GetHashCode([DisallowNull] TSelector obj) {
        if(obj is null)
            throw new ArgumentNullException(nameof(obj));

        return equalityComparer.GetHashCode(obj);
    }

    public bool Equals(TSelector? x, TSelector? y) =>
        equalityComparer.Equals(x, y);

    public bool IsAbsoluteOrdering([DisallowNull] TSelector selector) {
        if(selector is null)
            throw new ArgumentNullException(nameof(selector));

        if(equalityComparer is IOrderingSelectorComparer<TSelector> selectorComparer)
            return selectorComparer.IsAbsoluteOrdering(selector);
        if(selector is IOrderingSelectorLike selectorLike)
            return selectorLike.IsAbsoluteOrdering;

        return false;
    }
}
