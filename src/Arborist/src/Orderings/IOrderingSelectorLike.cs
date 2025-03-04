namespace Arborist.Orderings;

/// <summary>
/// Untyped representation of an <see cref="IOrderingSelector{TSelf}"/>. Permits access to
/// <see cref="IOrderingSelectorLike.IsAbsoluteOrdering"/> without imposing a type constraint on
/// the selector type.
/// </summary>
public interface IOrderingSelectorLike {
    /// <summary>
    /// Returns true if the selector represents an absolute ordering over the set of subject
    /// entities such that the order of the entities resulting from application of the ordering
    /// will always be the same regardless of the input order of the entities. A selector
    /// mapping to a database column with a unique constraint would typically represent an
    /// absolute ordering.
    /// </summary>
    public bool IsAbsoluteOrdering { get; }
}
