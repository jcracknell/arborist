using System.Diagnostics.Contracts;

namespace Arborist.Interpolation.InterceptorGenerator;

internal static class EnumerableExtensions {
    [Pure]
    public static HashCode AddRange<A>(
        this HashCode hash,
        IEnumerable<A> enumerable,
        IEqualityComparer<A>? equalityComparer = default
    ) {
        var length = 0;
        foreach(var element in enumerable) {
            hash.Add(element, equalityComparer);
            length += 1;
        }

        hash.Add(length);
        return hash;
    }

    [Pure]
    public static HashCode AddRange<A>(
        this HashCode hash,
        IReadOnlyList<A> list,
        IEqualityComparer<A>? equalityComparer = default
    ) {
        var length = list.Count;
        for(var i = 0; i < length; i++)
            hash.Add(list[i], equalityComparer);

        hash.Add(length);
        return hash;
    }

    [Pure]
    public static HashCode AddRange<A>(
        this HashCode hash,
        ReadOnlySpan<A> span,
        IEqualityComparer<A>? equalityComparer = default
    ) {
        var length = span.Length;
        for(var i = 0; i < length; i++)
            hash.Add(span[i], equalityComparer);

        hash.Add(length);
        return hash;
    }

    public static string MkString<A>(this IEnumerable<A> collection, string separator) =>
        MkString(collection, "", static a => a, separator, "");

    public static string MkString<A>(this IEnumerable<A> collection, string before, string separator, string after) =>
        MkString(collection, before, static a => a, separator, after);

    public static string MkString<A, B>(this IEnumerable<A> collection, Func<A, B> projection, string separator) =>
        MkString(collection, "", projection, separator, "");

    public static string MkString<A, B>(
        this IEnumerable<A> collection,
        string before,
        Func<A, B> projection,
        string separator,
        string after
    ) {
        using var enumerator = collection.GetEnumerator();

        using var writer = PooledStringWriter.Rent();
        writer.Write(before);

        if(enumerator.MoveNext()) {
            writer.Write(projection(enumerator.Current));
            while(enumerator.MoveNext()) {
                writer.Write(separator);
                writer.Write(projection(enumerator.Current));
            }
        }

        writer.Write(after);
        return writer.ToString();
    }

    public static IReadOnlyList<A> NullToEmpty<A>(this IReadOnlyList<A>? list) =>
        list ?? Array.Empty<A>();

    /// <summary>
    /// Eagerly applies the provided <paramref name="projection"/> to the subject <paramref name="collection"/>
    /// with known length, returning the results.
    /// </summary>
    public static B[] SelectEager<A, B>(
        this IReadOnlyCollection<A> collection,
        Func<A, B> projection
    ) {
        var count = collection.Count;
        if(count == 0)
            return Array.Empty<B>();

        var index = 0;
        var results = new B[count];
        if(collection is IReadOnlyList<A> list) {
            while(index < count) {
                results[index] = projection(list[index]);
                index += 1;
            }
        } else {
            foreach(var element in collection) {
                results[index] = projection(element);
                index += 1;
            }
        }

        return results;
    }

    /// <summary>
    /// Eagerly applies the provided <paramref name="projection"/> to the subject <paramref name="collection"/>
    /// with known length, returning the results.
    /// </summary>
    public static B[] SelectEager<A, B>(
        this IReadOnlyCollection<A> collection,
        Func<A, int, B> projection
    ) {
        var count = collection.Count;
        if(count == 0)
            return Array.Empty<B>();

        var index = 0;
        var results = new B[count];
        if(collection is IReadOnlyList<A> list) {
            while(index < count) {
                results[index] = projection(list[index], index);
                index += 1;
            }
        } else {
            foreach(var element in collection) {
                results[index] = projection(element, index);
                index += 1;
            }
        }

        return results;
    }

    public static bool TryGetFirst<A>(
        this IEnumerable<A> collection,
        [MaybeNullWhen(false)] out A result
    ) =>
        TryGetFirst(collection, static a => true, out result);

    public static bool TryGetFirst<A>(
        this IEnumerable<A> collection,
        Func<A, bool> predicate,
        [MaybeNullWhen(false)] out A result
    ) {
        using var enumerator = collection.GetEnumerator();

        while(enumerator.MoveNext()) {
            var element = enumerator.Current;
            if(predicate(element)) {
                result = element;
                return true;
            }
        }

        result = default;
        return false;
    }

    public static bool TryGetSingle<A>(
        this IEnumerable<A> collection,
        [MaybeNullWhen(false)] out A result
    ) =>
        TryGetSingle(collection, static a => true, out result);

    public static bool TryGetSingle<A>(
        this IEnumerable<A> collection,
        Func<A, bool> predicate,
        [MaybeNullWhen(false)] out A result
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

    public static IEnumerable<(A, B)> Zip<A, B>(this IEnumerable<A> enumerable0, IEnumerable<B> enumerable1) {
        using var enumerator0 = enumerable0.GetEnumerator();
        using var enumerator1 = enumerable1.GetEnumerator();

        while(enumerator0.MoveNext()) {
            if(!enumerator1.MoveNext())
                throw new InvalidOperationException();

            yield return (enumerator0.Current, enumerator1.Current);
        }

        if(enumerator1.MoveNext())
            throw new InvalidOperationException();
    }

    public static IEnumerable<(A Value, int Index)> ZipWithIndex<A>(this IEnumerable<A> enumerable) =>
        enumerable.Select(static (a, i) => (a, i));
}
