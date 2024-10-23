namespace Arborist;

public static class ExpressionThunk {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<R>> Of<R>(Expression<Func<R>> expression) =>
        expression;

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on <see cref="EI"/> with the corresponding subexpressions.
    /// </summary>
    /// <seealso cref="EI"/>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<R>> Interpolate<R>(Expression<Func<R>> expression) =>
        ExpressionHelpers.Interpolate(expression);
}
