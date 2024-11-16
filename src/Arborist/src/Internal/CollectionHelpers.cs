namespace Arborist.Internal;

public static class CollectionHelpers {
    /// <summary>
    /// Converts the provided argument <paramref name="enumerable"/> into an <see cref="IReadOnlyCollection{T}"/>,
    /// returning the input verbatim if it is already a collection.
    /// </summary>
    public static IReadOnlyCollection<A> AsReadOnlyCollection<A>(IEnumerable<A> enumerable) =>
        enumerable switch {
            IReadOnlyCollection<A> collection => collection,
            _ => enumerable.ToList()
        };

    /// <summary>
    /// Converts the provided argument <paramref name="enumerable"/> into an <see cref="IReadOnlyList{T}"/>,
    /// returning the input verbatim if it is already a list.
    /// </summary>
    public static IReadOnlyList<A> AsReadOnlyList<A>(IEnumerable<A> enumerable) =>
        enumerable switch {
            IReadOnlyList<A> list => list,
            IReadOnlyCollection<A> { Count: 0 } => Array.Empty<A>(),
            _ => enumerable.ToList()
        };
}
