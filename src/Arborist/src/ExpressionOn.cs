namespace Arborist;

public static class ExpressionOn {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<TResult>> Of<TResult>(Expression<Func<TResult>> expression) =>
        expression;
}
