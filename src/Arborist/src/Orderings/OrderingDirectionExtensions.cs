using System.Diagnostics.Contracts;

namespace Arborist.Orderings;

public static class OrderingDirectionExtensions {
    /// <summary>
    /// Applies the provided <paramref name="direction"/> to the subject <see cref="OrderingDirection"/>,
    /// returning the inverted value if the argument <paramref name="direction"/> is
    /// <see cref="OrderingDirection.Descending"/>.
    /// </summary>
    /// <seealso cref="Invert"/>
    [Pure]
    public static OrderingDirection ApplyDirection(
        this OrderingDirection subject,
        OrderingDirection direction
    ) =>
        OrderingDirection.Descending == direction ? subject.Invert() : subject;

    /// <summary>
    /// Inverts the subject <see cref="OrderingDirection"/>, returning the opposite value.
    /// </summary>
    [Pure]
    public static OrderingDirection Invert(this OrderingDirection direction) =>
        direction switch {
            OrderingDirection.Ascending => OrderingDirection.Descending,
            OrderingDirection.Descending => OrderingDirection.Ascending,
            _ => throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {direction}")
        };
}
