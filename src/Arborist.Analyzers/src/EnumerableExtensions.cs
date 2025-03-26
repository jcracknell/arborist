namespace Arborist.Analyzers;

internal static class EnumerableExtensions {
    public static IEnumerable<(A Value, int Index)> ZipWithIndex<A>(this IEnumerable<A> enumerable) =>
        enumerable.Select(static (a, i) => (a, i));
}
