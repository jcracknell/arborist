namespace Arborist.Orderings;

/// <summary>
/// Interface defining <see cref="Ordering{TSelector}"/> selector equality.
/// Used as the basis for simplification of orderings.
/// </summary>
/// <seealso cref="OrderingExtensions.Simplify{TSelector}(Ordering{TSelector},IOrderingSelectorComparer{TSelector})"/>
public interface IOrderingSelectorComparer<in TSelector> : IEqualityComparer<TSelector> {
    /// <summary>
    /// Returns true if the selector represents an absolute ordering over the set of subject
    /// entities such that the order of the entities resulting from application of the ordering
    /// will always be the same regardless of the input order of the entities. A selector
    /// mapping to a database column with a unique constraint would typically represent an
    /// absolute ordering.
    /// </summary>
    public bool IsAbsoluteOrdering([DisallowNull] TSelector selector);
}
