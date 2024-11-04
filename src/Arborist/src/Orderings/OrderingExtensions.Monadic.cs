namespace Arborist.Orderings;

public static partial class OrderingExtensions {
    /// <summary>
    /// Applies the provided <paramref name="projection"/> to the terms of the subject ordering,
    /// returning a new <see cref="Ordering{TSelector}"/> containing the results.
    /// </summary>
    public static Ordering<TProjected> Select<TSelector, TProjected>(
        this Ordering<TSelector> ordering,
        Func<OrderingTerm<TSelector>, OrderingTerm<TProjected>> projection
    ) {
        if(ordering.IsEmpty)
            return Ordering<TProjected>.Unordered;

        var builder = new OrderingBuilder<TProjected>();
        var rest = ordering;
        do {
            builder.Add(projection(rest.Term));
            rest = rest.Rest;
        } while(!rest.IsEmpty);

        return builder.Build();
    }

    /// <summary>
    /// Applies the provided <paramref name="projection"/> to the terms of the subject ordering,
    /// returning a new <see cref="Ordering{TSelector}"/> containing the results.
    /// </summary>
    public static Ordering<TProjected> SelectMany<TSelector, TProjected>(
        this Ordering<TSelector> ordering,
        Func<OrderingTerm<TSelector>, IEnumerable<OrderingTerm<TProjected>>> projection
    ) {
        if(ordering.IsEmpty)
            return OrderingNil<TProjected>.Instance;

        var builder = new OrderingBuilder<TProjected>();
        var rest = ordering;
        do {
            builder.AddRange(projection(rest.Term));
            rest = rest.Rest;
        } while(!rest.IsEmpty);

        return builder.Build();
    }

    /// <summary>
    /// Applies the provided <paramref name="projection"/> to the terms of the subject ordering,
    /// returning a new <see cref="Ordering{TSelector}"/> containing the results.
    /// </summary>
    public static Ordering<TResult> SelectMany<TSelector, TProjected, TResult>(
        this Ordering<TSelector> ordering,
        Func<OrderingTerm<TSelector>, IEnumerable<OrderingTerm<TProjected>>> projection,
        Func<OrderingTerm<TSelector>, OrderingTerm<TProjected>, OrderingTerm<TResult>> transform
    ) {
        if(ordering.IsEmpty)
            return OrderingNil<TResult>.Instance;

        var builder = new OrderingBuilder<TResult>();
        var rest = ordering;
        do {
            foreach(var term in projection(rest.Term))
                builder.Add(transform(rest.Term, term));

            rest = rest.Rest;
        } while(!rest.IsEmpty);

        return builder.Build();
    }

    /// <summary>
    /// Filters the terms of the subject ordering, returning a new <see cref="Ordering{TSelector}"/> containing only the
    /// terms satisfying the provided <paramref name="predicate"/>.
    /// </summary>
    public static Ordering<TSelector> Where<TSelector>(
        this Ordering<TSelector> ordering,
        Func<OrderingTerm<TSelector>, bool> predicate
    ) {
        if(ordering.IsEmpty)
            return OrderingNil<TSelector>.Instance;

        var builder = new OrderingBuilder<TSelector>();
        var rest = ordering;
        do {
            if(predicate(rest.Term))
                builder.Add(rest.Term);

            rest = rest.Rest;
        } while(!rest.IsEmpty);

        return builder.Build();
    }
}
