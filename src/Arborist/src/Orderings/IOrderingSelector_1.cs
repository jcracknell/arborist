namespace Arborist.Orderings;

/// <summary>
/// Interface defining functionality required for simplification of <see cref="Ordering{TSelector}"/>
/// instances based on the implementing selector type.
/// </summary>
public interface IOrderingSelector<TSelf> : IOrderingSelectorLike, IEquatable<TSelf>
    where TSelf : IOrderingSelector<TSelf>
{ }
