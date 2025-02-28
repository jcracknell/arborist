namespace Arborist;

public static partial class ExpressionOn<A, B, C, D> {
    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, R>> Graft<I, R>(
        Expression<Func<A, B, C, D, I>> root,
        Expression<Func<I, R>> branch
    ) =>
        Expression.Lambda<Func<A, B, C, D, R>>(
            body: ExpressionHelper.Replace(branch.Body, branch.Parameters[0], root.Body),
            parameters: root.Parameters
        );

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, R?>> GraftNullable<I, J, R>(
        Expression<Func<A, B, C, D, I>> root,
        Expression<Func<J, R>> branch
    )
        where I : J?
        where J : class
        where R : class? =>
        Expression.Lambda<Func<A, B, C, D, R?>>(
            body: Expression.Condition(
                Expression.Equal(root.Body, Expression.Constant(null)),
                Expression.Constant(null, typeof(R)),
                ExpressionHelper.Replace(
                    branch.Body,
                    branch.Parameters[0],
                    (typeof(I) == typeof(J)) switch {
                        true => root.Body,
                        false => Expression.Convert(root.Body, typeof(J))
                    }
                )
            ),
            parameters: root.Parameters
        );

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, C, D, Nullable<R>>> GraftNullable<I, J, R>(
        Expression<Func<A, B, C, D, I>> root,
        Expression<Func<J, R>> branch,
        Nullable<R> dummy = default
    )
        where I : J?
        where J : class
        where R : struct =>
        Expression.Lambda<Func<A, B, C, D, Nullable<R>>>(
            body: Expression.Condition(
                Expression.Equal(root.Body, Expression.Constant(null)),
                Expression.Constant(null, typeof(Nullable<R>)),
                Expression.Convert(
                    ExpressionHelper.Replace(
                        branch.Body,
                        branch.Parameters[0],
                        (typeof(I) == typeof(J)) switch {
                            true => root.Body,
                            false => Expression.Convert(root.Body, typeof(J))
                        }
                    ),
                    typeof(Nullable<R>)
                )
            ),
            parameters: root.Parameters
        );

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, Nullable<R>>> GraftNullable<I, J, R>(
        Expression<Func<A, B, C, D, I>> root,
        Expression<Func<J, Nullable<R>>> branch
    )
        where I : J?
        where J : class
        where R : struct =>
        Expression.Lambda<Func<A, B, C, D, Nullable<R>>>(
            body: Expression.Condition(
                Expression.Equal(root.Body, Expression.Constant(null)),
                Expression.Constant(null, typeof(Nullable<R>)),
                ExpressionHelper.Replace(
                    branch.Body,
                    branch.Parameters[0],
                    (typeof(I) == typeof(J)) switch {
                        true => root.Body,
                        false => Expression.Convert(root.Body, typeof(J))
                    }
                )
            ),
            parameters: root.Parameters
        );

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, R?>> GraftNullable<I, R>(
        Expression<Func<A, B, C, D, Nullable<I>>> root,
        Expression<Func<I, R>> branch
    )
        where I : struct
        where R : class? =>
        Expression.Lambda<Func<A, B, C, D, R?>>(
            body: Expression.Condition(
                Expression.Equal(root.Body, Expression.Constant(null, typeof(Nullable<I>))),
                Expression.Constant(null, typeof(R)),
                ExpressionHelper.Replace(
                    branch.Body,
                    branch.Parameters[0],
                    Expression.Property(root.Body, typeof(Nullable<I>).GetProperty(nameof(System.Nullable<I>.Value))!)
                )
            ),
            parameters: root.Parameters
        );

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, C, D, Nullable<R>>> GraftNullable<I, R>(
        Expression<Func<A, B, C, D, Nullable<I>>> root,
        Expression<Func<I, R>> branch,
        Nullable<R> dummy = default
    )
        where I : struct
        where R : struct =>
        Expression.Lambda<Func<A, B, C, D, Nullable<R>>>(
            body: Expression.Condition(
                Expression.Equal(root.Body, Expression.Constant(null, typeof(Nullable<I>))),
                Expression.Constant(null, typeof(Nullable<R>)),
                Expression.Convert(
                    ExpressionHelper.Replace(
                        branch.Body,
                        branch.Parameters[0],
                        Expression.Property(root.Body, typeof(Nullable<I>).GetProperty(nameof(System.Nullable<I>.Value))!)
                    ),
                    typeof(Nullable<R>)
                )
            ),
            parameters: root.Parameters
        );

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, Nullable<R>>> GraftNullable<I, R>(
        Expression<Func<A, B, C, D, Nullable<I>>> root,
        Expression<Func<I, Nullable<R>>> branch
    )
        where I : struct
        where R : struct =>
        Expression.Lambda<Func<A, B, C, D, Nullable<R>>>(
            body: Expression.Condition(
                Expression.Equal(root.Body, Expression.Constant(null, typeof(Nullable<I>))),
                Expression.Constant(null, typeof(Nullable<R>)),
                ExpressionHelper.Replace(
                    branch.Body,
                    branch.Parameters[0],
                    Expression.Property(root.Body, typeof(Nullable<I>).GetProperty(nameof(System.Nullable<I>.Value))!)
                )
            ),
            parameters: root.Parameters
        );
}
