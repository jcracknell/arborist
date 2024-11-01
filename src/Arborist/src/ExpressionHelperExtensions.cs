namespace Arborist;

public static partial class ExpressionHelperExtensions {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<R>> Of<R>(
        this IExpressionHelperOnNone helper,
        Expression<Func<R>> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action> Of<R>(
        this IExpressionHelperOnNone helper,
        Expression<Action> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, R>> Of<A, R>(
        this IExpressionHelperOn<A> helper,
        Expression<Func<A, R>> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A>> Of<A>(
        this IExpressionHelperOn<A> helper,
        Expression<Action<A>> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, B, R>> Of<A, B, R>(
        this IExpressionHelperOn<A, B> helper,
        Expression<Func<A, B, R>> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A, B>> Of<A, B>(
        this IExpressionHelperOn<A, B> helper,
        Expression<Action<A, B>> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, B, C, R>> Of<A, B, C, R>(
        this IExpressionHelperOn<A, B, C> helper,
        Expression<Func<A, B, C, R>> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A, B, C>> Of<A, B, C>(
        this IExpressionHelperOn<A, B, C> helper,
        Expression<Action<A, B, C>> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, B, C, D, R>> Of<A, B, C, D, R>(
        this IExpressionHelperOn<A, B, C, D> helper,
        Expression<Func<A, B, C, D, R>> expression
    ) =>
        expression;
        
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A, B, C, D>> Of<A, B, C, D>(
        this IExpressionHelperOn<A, B, C, D> helper,
        Expression<Action<A, B, C, D>> expression
    ) =>
        expression;
}
