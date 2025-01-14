using System.Text;

namespace Arborist.CodeGen;

internal static class EnumerableExtensions {
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

        var sb = new StringBuilder();
        sb.Append(before);

        if(enumerator.MoveNext()) {
            sb.Append(projection(enumerator.Current));
            while(enumerator.MoveNext()) {
                sb.Append(separator);
                sb.Append(projection(enumerator.Current));
            }
        }

        sb.Append(after);
        return sb.ToString();
    }

    public static IReadOnlyList<A> NullToEmpty<A>(this IReadOnlyList<A>? list) =>
        list ?? Array.Empty<A>();

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
