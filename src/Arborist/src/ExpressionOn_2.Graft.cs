namespace Arborist;

public static partial class ExpressionOn<A, B> {
    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, R>> Graft<I, R>(
        Expression<Func<A, B, I>> root,
        Expression<Func<I, R>> branch
    ) =>
        ExpressionHelper.Graft(root, branch);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, R?>> GraftNullable<I, J, R>(
        Expression<Func<A, B, I>> root,
        Expression<Func<J, R>> branch
    )
        where I : class?, J?
        where J : class?
        where R : class? =>
        ExpressionHelper.GraftNullable(root, branch);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, Nullable<R>>> GraftNullable<I, J, R>(
        Expression<Func<A, B, I>> root,
        Expression<Func<J, R>> branch,
        Nullable<R> dummy = default
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        ExpressionHelper.GraftNullable(root, branch);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, Nullable<R>>> GraftNullable<I, J, R>(
        Expression<Func<A, B, I>> root,
        Expression<Func<J, Nullable<R>>> branch
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        ExpressionHelper.GraftNullable(root, branch);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, R?>> GraftNullable<I, R>(
        Expression<Func<A, B, Nullable<I>>> root,
        Expression<Func<I, R>> branch
    )
        where I : struct
        where R : class? =>
        ExpressionHelper.GraftNullable(root, branch);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, Nullable<R>>> GraftNullable<I, R>(
        Expression<Func<A, B, Nullable<I>>> root,
        Expression<Func<I, R>> branch,
        Nullable<R> dummy = default
    )
        where I : struct
        where R : struct =>
        ExpressionHelper.GraftNullable(root, branch);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, Nullable<R>>> GraftNullable<I, R>(
        Expression<Func<A, B, Nullable<I>>> root,
        Expression<Func<I, Nullable<R>>> branch
    )
        where I : struct
        where R : struct =>
        ExpressionHelper.GraftNullable(root, branch);
}
