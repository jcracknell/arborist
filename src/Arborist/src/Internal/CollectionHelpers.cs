namespace Arborist.Internal;

internal static class CollectionHelpers {
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

    /// <summary>
    /// Eagerly applies the provided <paramref name="projection"/> to the elements of the subject
    /// collection, returning the results as an array.
    /// </summary>
    public static B[] SelectEager<A, B>(IReadOnlyCollection<A> collection, Func<A, B> projection) {
        if(collection is IReadOnlyList<A> list)
            return SelectEager(list, projection);

        var count = collection.Count;
        if(collection.Count == 0)
            return Array.Empty<B>();

        var results = new B[count];

        using var enumerator = collection.GetEnumerator();

        for(var i = 0; i < count; i++) {
            if(!enumerator.MoveNext())
                throw new InvalidOperationException($"Subject {nameof(collection)} has fewer elements than the expected {nameof(collection.Count)}.");

            results[i] = projection(enumerator.Current);
        }

        if(enumerator.MoveNext())
            throw new InvalidOperationException($"Subject {nameof(collection)} contains elements beyond the expected {nameof(collection.Count)}.");

        return results;
    }

    /// <summary>
    /// Eagerly applies the provided <paramref name="projection"/> to the elements of the subject
    /// collection, returning the results as an array.
    /// </summary>
    public static B[] SelectEager<A, B>(IReadOnlyList<A> list, Func<A, B> projection) {
        var count = list.Count;
        if(count == 0)
            return Array.Empty<B>();

        var results = new B[count];
        for(var i = 0; i < count; i++)
            results[i] = projection(list[i]);

        return results;
    }
}
