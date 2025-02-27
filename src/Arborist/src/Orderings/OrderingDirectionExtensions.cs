namespace Arborist.Orderings;

public static class OrderingDirectionExtensions {
    /// <summary>
    /// Inverts the subject <see cref="OrderingDirection"/>, returning the opposite value.
    /// </summary>
    public static OrderingDirection InvertDirection(this OrderingDirection direction) =>
        direction switch {
            OrderingDirection.Ascending => OrderingDirection.Descending,
            OrderingDirection.Descending => OrderingDirection.Ascending,
            _ => throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {direction}")
        };
}
