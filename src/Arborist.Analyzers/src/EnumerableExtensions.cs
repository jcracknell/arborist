namespace Arborist.Analyzers;

internal static class EnumerableExtensions {
    public static bool TryGetSingle<A>(
        this IEnumerable<A> collection,
        [NotNullWhen(true)] out A? result
    ) =>
        TryGetSingle(collection, static a => true, out result);

    public static bool TryGetSingle<A>(
        this IEnumerable<A> collection,
        Func<A, bool> predicate,
        [NotNullWhen(true)] out A? result
    ) {
        result = default;
        var found = false;

        using var enumerator = collection.GetEnumerator();

        while(enumerator.MoveNext()) {
            var element = enumerator.Current;
            if(predicate(element)) {
                if(found)
                    throw new InvalidOperationException("Sequence contains multiple matching elements.");

                result = element;
                found = true;
            }
        }

        return found;
    }

    public static IEnumerable<(A Value, int Index)> ZipWithIndex<A>(this IEnumerable<A> enumerable) =>
        enumerable.Select(static (a, i) => (a, i));
}
