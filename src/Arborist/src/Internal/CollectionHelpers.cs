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

    /// <summary>
    /// Copies the specified number of values from the provided <paramref name="source"/> list to the provided
    /// <paramref name="destination"/> list starting from the provided offsets.
    /// </summary>
    public static void Copy<A>(
        IReadOnlyList<A> source,
        int sourceOffset,
        IList<A> destination,
        int destinationOffset,
        int count
    ) {
        for(var i = 0; i < count; i++)
            destination[destinationOffset + i] = source[sourceOffset + i];
    }
}
