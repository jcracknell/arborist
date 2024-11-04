using Arborist.Orderings.JsonConverters;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Arborist.Orderings;

/// <summary>
/// Defines an ordering specified as a collection of <see cref="OrderingTerm{TSelector}"/> instances
/// pairing a selector of type <typeparamref name="TSelector"/> with an <see cref="OrderingDirection"/>.
/// </summary>
/// <remarks>
/// This is defined as an interface (and implemented as a singly-linked list) because it is vitally
/// important that this type by covariant on its type parameter in order to facilitate the use of composite
/// selectors implemented as algebraic data types (typically subclasses).
/// </remarks>
/// <seealso cref="Ordering"/>
/// <seealso cref="OrderingTerm{TSelector}"/>
[CollectionBuilder(typeof(Ordering), nameof(Ordering.By))]
[JsonConverter(typeof(OrderingJsonConverterFactory))]
public interface Ordering<out TSelector> : IEnumerable<OrderingTerm<TSelector>>, IOrderingLike, IEquatable<IOrderingLike> {
    /// <summary>
    /// An empty <see cref="Ordering{TSelector}"/> containing no terms.
    /// </summary>
    public static Ordering<TSelector> Unordered =>
        OrderingNil<TSelector>.Instance;

    /// <summary>
    /// Creates a single-term <see cref="Ordering{TSelector}"/> using the specified <paramref name="selector"/>
    /// and <paramref name="direction"/>.
    /// </summary>
    public static Ordering<TSelector> By(TSelector selector, OrderingDirection direction) =>
        By(OrderingTerm.Create(selector, direction));

    /// <summary>
    /// Creates an <see cref="Ordering{TSelector}"/> containing the specified <paramref name="term"/>.
    /// </summary>
    public static Ordering<TSelector> By(OrderingTerm<TSelector> term) =>
        new OrderingCons<TSelector>(term, OrderingNil<TSelector>.Instance);

    /// <summary>
    /// Creates an <see cref="Ordering{TSelector}"/> using the specified <paramref name="terms"/>.
    /// </summary>
    public static Ordering<TSelector> By(IEnumerable<OrderingTerm<TSelector>> terms) {
        var builder = new OrderingBuilder<TSelector>();
        builder.AddRange(terms);
        return builder.Build();
    }


    /// <summary>
    /// Creates an <see cref="Ordering{TSelector}"/> using the specified <paramref name="terms"/>.
    /// </summary>
    public static Ordering<TSelector> By(params OrderingTerm<TSelector>[] terms) =>
        By(terms.AsSpan());

    /// <summary>
    /// Creates an <see cref="Ordering{TSelector}"/> using the specified <paramref name="terms"/>.
    /// </summary>
    public static Ordering<TSelector> By(ReadOnlySpan<OrderingTerm<TSelector>> terms) {
        var builder = new OrderingBuilder<TSelector>();
        builder.AddRange(terms);
        return builder.Build();
    }

    /// <summary>
    /// Creates a single term <see cref="Ordering{TSelector}"/> using the specified <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Ascending"/> direction.
    /// </summary>
    public static Ordering<TSelector> ByAscending(TSelector selector) =>
        By(selector, OrderingDirection.Ascending);

    /// <summary>
    /// Creates a single term <see cref="Ordering{TSelector}"/> using the specified <paramref name="selector"/>
    /// sorted in the <see cref="OrderingDirection.Descending"/> direction.
    /// </summary>
    public static Ordering<TSelector> ByDescending(TSelector selector) =>
        By(selector, OrderingDirection.Ascending);

    /// <summary>
    /// The first <see cref="OrderingTerm{TSelector}"/> in the ordering.
    /// Throws an <see cref="InvalidOperationException"/> if the ordering <see cref="IOrderingLike.IsEmpty"/>.
    /// </summary>
    /// <exception name="InvalidOperationException">
    /// Thrown if this ordering <see cref="IOrderingLike.IsEmpty"/>.
    /// </exception>
    public new OrderingTerm<TSelector> Term { get; }


    /// <summary>
    /// Any remaining terms after the first <see cref="Term"/> in the ordering.
    /// Throws an <see cref="InvalidOperationException"/> if the ordering <see cref="IOrderingLike.IsEmpty"/>.
    /// </summary>
    /// <exception name="InvalidOperationException">
    /// Thrown if this ordering <see cref="IOrderingLike.IsEmpty"/>.
    /// </exception>
    public new Ordering<TSelector> Rest { get; }
}
