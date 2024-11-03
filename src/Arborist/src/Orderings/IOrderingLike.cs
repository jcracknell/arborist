namespace Arborist.Orderings;

public interface IOrderingLike {
    public IOrderingTermLike Term { get; }
    public IOrderingLike Rest { get; }

    /// <summary>
    /// True if this ordering is empty and contains no terms.
    /// </summary>
    public bool IsEmpty { get; }
}
