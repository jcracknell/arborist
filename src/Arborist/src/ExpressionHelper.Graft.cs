namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<R>> Graft<I, R>(
        Expression<Func<I>> root,
        Expression<Func<I, R>> branch
    ) =>
        GraftImpl<Func<R>>(root, branch);

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, R>> Graft<A, I, R>(
        Expression<Func<A, I>> root,
        Expression<Func<I, R>> branch
    ) =>
        GraftImpl<Func<A, R>>(root, branch);

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, R>> Graft<A, B, I, R>(
        Expression<Func<A, B, I>> root,
        Expression<Func<I, R>> branch
    ) =>
        GraftImpl<Func<A, B, R>>(root, branch);

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, R>> Graft<A, B, C, I, R>(
        Expression<Func<A, B, C, I>> root,
        Expression<Func<I, R>> branch
    ) =>
        GraftImpl<Func<A, B, C, R>>(root, branch);

    /// <summary>
    /// Grafts the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, R>> Graft<A, B, C, D, I, R>(
        Expression<Func<A, B, C, D, I>> root,
        Expression<Func<I, R>> branch
    ) =>
        GraftImpl<Func<A, B, C, D, R>>(root, branch);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<R?>> GraftNullable<I, J, R>(
        Expression<Func<I>> root,
        Expression<Func<J, R>> branch
    )
        where I : class?, J?
        where J : class?
        where R : class? =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, R?>> GraftNullable<A, I, J, R>(
        Expression<Func<A, I>> root,
        Expression<Func<J, R>> branch
    )
        where I : class?, J?
        where J : class?
        where R : class? =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, R?>> GraftNullable<A, B, I, J, R>(
        Expression<Func<A, B, I>> root,
        Expression<Func<J, R>> branch
    )
        where I : class?, J?
        where J : class?
        where R : class? =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, R?>> GraftNullable<A, B, C, I, J, R>(
        Expression<Func<A, B, C, I>> root,
        Expression<Func<J, R>> branch
    )
        where I : class?, J?
        where J : class?
        where R : class? =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, R?>> GraftNullable<A, B, C, D, I, J, R>(
        Expression<Func<A, B, C, D, I>> root,
        Expression<Func<J, R>> branch
    )
        where I : class?, J?
        where J : class?
        where R : class? =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<Nullable<R>>> GraftNullable<I, J, R>(
        Expression<Func<I>> root,
        Expression<Func<J, R>> branch,
        Nullable<R> dummy = default
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, Nullable<R>>> GraftNullable<A, I, J, R>(
        Expression<Func<A, I>> root,
        Expression<Func<J, R>> branch,
        Nullable<R> dummy = default
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, Nullable<R>>> GraftNullable<A, B, I, J, R>(
        Expression<Func<A, B, I>> root,
        Expression<Func<J, R>> branch,
        Nullable<R> dummy = default
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, C, Nullable<R>>> GraftNullable<A, B, C, I, J, R>(
        Expression<Func<A, B, C, I>> root,
        Expression<Func<J, R>> branch,
        Nullable<R> dummy = default
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, C, D, Nullable<R>>> GraftNullable<A, B, C, D, I, J, R>(
        Expression<Func<A, B, C, D, I>> root,
        Expression<Func<J, R>> branch,
        Nullable<R> dummy = default
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, R>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<Nullable<R>>> GraftNullable<I, J, R>(
        Expression<Func<I>> root,
        Expression<Func<J, Nullable<R>>> branch
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, Nullable<R>>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, Nullable<R>>> GraftNullable<A, I, J, R>(
        Expression<Func<A, I>> root,
        Expression<Func<J, Nullable<R>>> branch
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, Nullable<R>>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, Nullable<R>>> GraftNullable<A, B, I, J, R>(
        Expression<Func<A, B, I>> root,
        Expression<Func<J, Nullable<R>>> branch
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, Nullable<R>>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, Nullable<R>>> GraftNullable<A, B, C, I, J, R>(
        Expression<Func<A, B, C, I>> root,
        Expression<Func<J, Nullable<R>>> branch
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, Nullable<R>>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, Nullable<R>>> GraftNullable<A, B, C, D, I, J, R>(
        Expression<Func<A, B, C, D, I>> root,
        Expression<Func<J, Nullable<R>>> branch
    )
        where I : class?, J?
        where J : class?
        where R : struct =>
        Graft(root, NullConditional(GraftNullableRefBranch<I, J, Nullable<R>>(branch))!);

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<R?>> GraftNullable<I, R>(
        Expression<Func<Nullable<I>>> root,
        Expression<Func<I, R>> branch
    )
        where I : struct
        where R : class? =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, R?>> GraftNullable<A, I, R>(
        Expression<Func<A, Nullable<I>>> root,
        Expression<Func<I, R>> branch
    )
        where I : struct
        where R : class? =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, R?>> GraftNullable<A, B, I, R>(
        Expression<Func<A, B, Nullable<I>>> root,
        Expression<Func<I, R>> branch
    )
        where I : struct
        where R : class? =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, R?>> GraftNullable<A, B, C, I, R>(
        Expression<Func<A, B, C, Nullable<I>>> root,
        Expression<Func<I, R>> branch
    )
        where I : struct
        where R : class? =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, R?>> GraftNullable<A, B, C, D, I, R>(
        Expression<Func<A, B, C, D, Nullable<I>>> root,
        Expression<Func<I, R>> branch
    )
        where I : struct
        where R : class? =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<Nullable<R>>> GraftNullable<I, R>(
        Expression<Func<Nullable<I>>> root,
        Expression<Func<I, R>> branch,
        Nullable<R> dummy = default
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, Nullable<R>>> GraftNullable<A, I, R>(
        Expression<Func<A, Nullable<I>>> root,
        Expression<Func<I, R>> branch,
        Nullable<R> dummy = default
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, Nullable<R>>> GraftNullable<A, B, I, R>(
        Expression<Func<A, B, Nullable<I>>> root,
        Expression<Func<I, R>> branch,
        Nullable<R> dummy = default
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, C, Nullable<R>>> GraftNullable<A, B, C, I, R>(
        Expression<Func<A, B, C, Nullable<I>>> root,
        Expression<Func<I, R>> branch,
        Nullable<R> dummy = default
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, B, C, D, Nullable<R>>> GraftNullable<A, B, C, D, I, R>(
        Expression<Func<A, B, C, D, Nullable<I>>> root,
        Expression<Func<I, R>> branch,
        Nullable<R> dummy = default
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<Nullable<R>>> GraftNullable<I, R>(
        Expression<Func<Nullable<I>>> root,
        Expression<Func<I, Nullable<R>>> branch
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, Nullable<R>>> GraftNullable<A, I, R>(
        Expression<Func<A, Nullable<I>>> root,
        Expression<Func<I, Nullable<R>>> branch
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, Nullable<R>>> GraftNullable<A, B, I, R>(
        Expression<Func<A, B, Nullable<I>>> root,
        Expression<Func<I, Nullable<R>>> branch
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, Nullable<R>>> GraftNullable<A, B, C, I, R>(
        Expression<Func<A, B, C, Nullable<I>>> root,
        Expression<Func<I, Nullable<R>>> branch
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    /// <summary>
    /// Creates a ternary expression where the "then" arm produces null in the event that the
    /// body of the <paramref name="root"/> expression is null, and the "else" arm
    /// is the result of replacing the parameter to the <paramref name="branch"/> expression with
    /// the non-null body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, B, C, D, Nullable<R>>> GraftNullable<A, B, C, D, I, R>(
        Expression<Func<A, B, C, D, Nullable<I>>> root,
        Expression<Func<I, Nullable<R>>> branch
    )
        where I : struct
        where R : struct =>
        Graft(root, NullConditional(branch));

    internal static Expression<TDelegate> GraftImpl<TDelegate>(
        LambdaExpression root,
        LambdaExpression branch
    ) {
        AssertFuncType(typeof(TDelegate));

        return Expression.Lambda<TDelegate>(
            body: Replace(branch.Body, branch.Parameters[0], root.Body),
            parameters: root.Parameters
        );
    }

    internal static Expression<Func<I, R>> GraftNullableRefBranch<I, J, R>(Expression<Func<J, R>> branch)
        where I : class?, J?
        where J : class?
    {
        if(typeof(I) == typeof(J))
            return (Expression<Func<I, R>>)(LambdaExpression)branch;

        var parameter = Expression.Parameter(typeof(I), branch.Parameters[0].Name);

        return Expression.Lambda<Func<I, R>>(
            Replace(branch.Body, branch.Parameters[0], Expression.Convert(parameter, typeof(J))),
            parameter
        );
    }
}
