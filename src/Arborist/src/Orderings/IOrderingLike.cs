namespace Arborist.Orderings;

/// <summary>
/// Untyped representation of an <see cref="Ordering{TSelector}"/>.
/// Permits implementation of <see cref="IEquatable{T}"/>.
/// </summary>
public interface IOrderingLike : IEquatable<IOrderingLike> {
    public IOrderingTermLike Term { get; }
    public IOrderingLike Rest { get; }

    /// <summary>
    /// True if this ordering is empty and contains no terms.
    /// </summary>
    public bool IsEmpty { get; }
}
