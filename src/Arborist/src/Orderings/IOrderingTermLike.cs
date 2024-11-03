namespace Arborist.Orderings;

public interface IOrderingTermLike {
    public object? Selector { get; }

    /// <summary>
    /// The direction in which the ordering <see cref="Selector"/> should be applied.
    /// </summary>
    public OrderingDirection Direction { get; }
}
