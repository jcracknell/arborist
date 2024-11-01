namespace Arborist;

public static partial class ExpressionHelperExtensions {
    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<R>> Graft<A, R>(
        this IExpressionHelperOnNone helper,
        Expression<Func<A>> root,
        Expression<Func<A, R>> branch
    ) =>
        Expression.Lambda<Func<R>>(
            body: ExpressionHelper.Replace(branch.Body, branch.Parameters[0], root.Body),
            parameters: root.Parameters
        );
        
    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, R>> Graft<A, B, R>(
        this IExpressionHelperOn<A> helper,
        Expression<Func<A, B>> root,
        Expression<Func<B, R>> branch
    ) =>
        Expression.Lambda<Func<A, R>>(
            body: ExpressionHelper.Replace(branch.Body, branch.Parameters[0], root.Body),
            parameters: root.Parameters
        );
}
