namespace Arborist;

/// <typeparam name="A">
/// The type of the expression parameter.
/// </typeparam>
public static class ExpressionOn<A> {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<A, R>> Of<R>(Expression<Func<A, R>> expression) =>
        expression;

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on <see cref="EI"/> with the corresponding subexpressions.
    /// </summary>
    /// <seealso cref="EI"/>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<A, R>> Interpolate<R>(Expression<Func<A, R>> expression) =>
        ExpressionHelpers.Interpolate(expression);
}
