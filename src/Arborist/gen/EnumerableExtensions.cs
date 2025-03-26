namespace Arborist.Generators;

internal static class EnumerableExtensions {
    public static int IndexOf<A>(this IEnumerable<A> enumerable, Func<A, bool> predicate) {
        var index = 0;
        foreach(var element in enumerable) {
            if(predicate(element))
                return index;

            index += 1;
        }

        return -1;
    }
}
