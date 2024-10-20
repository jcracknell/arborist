namespace Arborist;

public static class ExpressionOn<P0> {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<P0, TResult>> Of<TResult>(Expression<Func<P0, TResult>> expression) =>
        expression;
}
