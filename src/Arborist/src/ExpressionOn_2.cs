namespace Arborist;

public static class ExpressionOn<A, B> {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, B, R>> Of<R>(Expression<Func<A, B, R>> expression) =>
        expression;

    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A, B>> Of(Expression<Action<A, B>> expression) =>
        expression;

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, RR>> Graft<R, RR>(
        Expression<Func<A, B, R>> root,
        Expression<Func<R, RR>> branch
    ) =>
        Expression.Lambda<Func<A, B, RR>>(
            body: ExpressionHelper.Replace(branch.Body, branch.Parameters[0], root.Body),
            parameters: root.Parameters
        );
}
