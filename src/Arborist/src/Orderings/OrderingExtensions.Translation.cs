namespace Arborist.Orderings;

public static partial class OrderingExtensions {
    /// <summary>
    /// Grafts the selector expressions of the subject ordering onto the provided
    /// <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">
    /// The root expression onto which the selector expressions will be grafted.
    /// </param>
    public static Ordering<Expression<Func<A, R>>> GraftSelectorExpressionsTo<A, I, R>(
        this Ordering<Expression<Func<I, R>>> ordering,
        Expression<Func<A, I>> expression
    ) =>
        GraftSelectorExpressions(
            ordering,
            expression,
            static (r, b) => ExpressionOn<A>.Graft(r, b)
        );

    /// <summary>
    /// Grafts the selector expressions of the subject ordering onto the provided null-returning
    /// <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">
    /// The root expression onto which the selector expressions will be grafted.
    /// </param>
    public static Ordering<Expression<Func<A, R?>>> GraftSelectorExpressionsToNullable<A, I, J, R>(
        this Ordering<Expression<Func<J, R>>> ordering,
        Expression<Func<A, I>> expression
    )
        where I : J?
        where J : class?
        where R : class? =>
        GraftSelectorExpressions(
            ordering,
            expression,
            static (r, b) => ExpressionOn<A>.GraftNullable(r, b)
        );

    /// <summary>
    /// Grafts the selector expressions of the subject ordering onto the provided null-returning
    /// <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">
    /// The root expression onto which the selector expressions will be grafted.
    /// </param>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Ordering<Expression<Func<A, Nullable<R>>>> GraftSelectorExpressionsToNullable<A, I, J, R>(
        this Ordering<Expression<Func<J, R>>> ordering,
        Expression<Func<A, I>> expression,
        Nullable<R> dummy = default
    )
        where I : J?
        where J : class?
        where R : struct =>
        GraftSelectorExpressions(
            ordering,
            expression,
            static (r, b) => ExpressionOn<A>.GraftNullable(r, b)
        );

    /// <summary>
    /// Grafts the selector expressions of the subject ordering onto the provided null-returning
    /// <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">
    /// The root expression onto which the selector expressions will be grafted.
    /// </param>
    public static Ordering<Expression<Func<A, Nullable<R>>>> GraftSelectorExpressionsToNullable<A, I, J, R>(
        this Ordering<Expression<Func<J, Nullable<R>>>> ordering,
        Expression<Func<A, I>> expression
    )
        where I : J?
        where J : class?
        where R : struct =>
        GraftSelectorExpressions(
            ordering,
            expression,
            static (r, b) => ExpressionOn<A>.GraftNullable(r, b)
        );

    /// <summary>
    /// Grafts the selector expressions of the subject ordering onto the provided null-returning
    /// <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">
    /// The root expression onto which the selector expressions will be grafted.
    /// </param>
    public static Ordering<Expression<Func<A, R?>>> GraftSelectorExpressionsToNullable<A, I, R>(
        this Ordering<Expression<Func<I, R>>> ordering,
        Expression<Func<A, Nullable<I>>> expression
    )
        where I : struct
        where R : class? =>
        GraftSelectorExpressions(
            ordering,
            expression,
            static (r, b) => ExpressionOn<A>.GraftNullable(r, b)
        );

    /// <summary>
    /// Grafts the selector expressions of the subject ordering onto the provided null-returning
    /// <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">
    /// The root expression onto which the selector expressions will be grafted.
    /// </param>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Ordering<Expression<Func<A, Nullable<R>>>> GraftSelectorExpressionsToNullable<A, I, R>(
        this Ordering<Expression<Func<I, R>>> ordering,
        Expression<Func<A, Nullable<I>>> expression,
        Nullable<R> dummy = default
    )
        where I : struct
        where R : struct =>
        GraftSelectorExpressions(
            ordering,
            expression,
            static (r, b) => ExpressionOn<A>.GraftNullable(r, b)
        );

    /// <summary>
    /// Grafts the selector expressions of the subject ordering onto the provided null-returning
    /// <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">
    /// The root expression onto which the selector expressions will be grafted.
    /// </param>
    public static Ordering<Expression<Func<A, Nullable<R>>>> GraftSelectorExpressionsToNullable<A, I, R>(
        this Ordering<Expression<Func<I, Nullable<R>>>> ordering,
        Expression<Func<A, Nullable<I>>> expression
    )
        where I : struct
        where R : struct =>
        GraftSelectorExpressions(
            ordering,
            expression,
            static (r, b) => ExpressionOn<A>.GraftNullable(r, b)
        );

    private static Ordering<Expression<Func<A, S>>> GraftSelectorExpressions<A, I, J, R, S>(
        Ordering<Expression<Func<J, R>>> ordering,
        Expression<Func<A, I>> expression,
        Func<Expression<Func<A, I>>, Expression<Func<J, R>>, Expression<Func<A, S>>> graft
    ) {
        if(ordering.IsEmpty)
            return Ordering<Expression<Func<A, S>>>.Unordered;

        var rest = ordering;
        var builder = new OrderingBuilder<Expression<Func<A, S>>>();
        do {
            builder.Add(OrderingTerm.Create(
                selector: graft(expression, rest.Term.Selector),
                direction: rest.Term.Direction
            ));

            rest = rest.Rest;
        } while(!rest.IsEmpty);

        return builder.Build();
    }

    /// <summary>
    /// Applies the provided selector <paramref name="translation"/> function to the selectors of the
    /// subject ordering, ensuring that the <see cref="OrderingDirection"/> of the input terms is
    /// correctly applied to the resulting terms.
    /// </summary>
    /// <param name="translation">
    /// Translation function converting <typeparamref name="TSelector"/> values into translated
    /// orderings for the <see cref="OrderingDirection.Ascending"/> direction.
    /// </param>
    public static Ordering<TResult> TranslateSelectors<TSelector, TResult>(
        this Ordering<TSelector> ordering,
        Func<TSelector, IEnumerable<OrderingTerm<TResult>>> translation
    ) =>
        ordering.TranslateSelectors(default(object?), (data, selector) => translation(selector));

    /// <summary>
    /// Applies the provided selector <paramref name="translation"/> function to the selectors of the
    /// subject ordering, ensuring that the <see cref="OrderingDirection"/> of the input terms is
    /// correctly applied to the resulting terms.
    /// </summary>
    /// <param name="data">
    /// Data provided to the translation function.
    /// Typically this can be used to pass an EntityFramework DbContext.
    /// </param>
    /// <param name="translation">
    /// Translation function converting <typeparamref name="TSelector"/> values into translated
    /// orderings for the <see cref="OrderingDirection.Ascending"/> direction.
    /// </param>
    public static Ordering<TResult> TranslateSelectors<TSelector, D, TResult>(
        this Ordering<TSelector> ordering,
        D data,
        Func<D, TSelector, IEnumerable<OrderingTerm<TResult>>> translation
    ) {
        if(ordering.IsEmpty)
            return Ordering<TResult>.Unordered;

        var rest = ordering;
        var builder = new OrderingBuilder<TResult>();
        do {
            foreach(var term in translation(data, rest.Term.Selector))
                builder.Add(term.ApplyDirection(rest.Term.Direction));

            rest = rest.Rest;
        } while(!rest.IsEmpty);

        return builder.Build();
    }
}
