namespace Arborist.Orderings;

public static class OrderingSelectorComparer<TSelector> {
    /// <summary>
    /// Returns a default <see cref="IOrderingSelectorComparer{TSelector}"/> based on the default
    /// <see cref="IEqualityComparer{T}"/> for the type specified by the generic argument.
    /// </summary>
    public static IOrderingSelectorComparer<TSelector> Default { get; } =
        new DefaultOrderingSelectorComparer<TSelector>(EqualityComparer<TSelector>.Default);
}
