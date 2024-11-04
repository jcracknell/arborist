namespace Arborist.Orderings;

public static class OrderingDirectionExtensions {
    /// <summary>
    /// Returns the opposite <see cref="OrderingDirection"/> value.
    /// </summary>
    public static OrderingDirection Reversed(this OrderingDirection direction) =>
        direction switch {
            OrderingDirection.Ascending => OrderingDirection.Descending,
            OrderingDirection.Descending => OrderingDirection.Ascending,
            _ => throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {direction}")
        };
}
