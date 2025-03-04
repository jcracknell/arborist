namespace Arborist.Orderings;

/// <summary>
/// Abstract base class for record-based <see cref="IOrderingSelector{TSelf}"/> implementations.
/// </summary>
public abstract record RecordOrderingSelector<TSelf> : IOrderingSelector<TSelf>
    where TSelf : RecordOrderingSelector<TSelf>
{
    protected virtual bool IsAbsoluteOrdering => false;

    // Explicit interface implementation to hide the property from casual inspection/serialization
    bool IOrderingSelectorLike.IsAbsoluteOrdering => IsAbsoluteOrdering;

    // Forward IEquatable<TSelf>.Equals to the derived record equality implementation
    public bool Equals(TSelf? that) =>
        Equals((RecordOrderingSelector<TSelf>?)that);
}
