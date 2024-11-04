namespace Arborist.Orderings;

/// <summary>
/// Untyped representation of an <see cref="OrderingTerm{TSelector}"/>.
/// Permits implementation of <see cref="IEquatable{T}"/>.
/// </summary>
public interface IOrderingTermLike : IEquatable<IOrderingTermLike> {
    public object? Selector { get; }

    /// <summary>
    /// The direction in which the ordering <see cref="Selector"/> should be applied.
    /// </summary>
    public OrderingDirection Direction { get; }
}
