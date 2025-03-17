namespace Arborist;

public static partial class ExpressionHelper {
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
    public static Expression<Func<A, R?>> GraftNullable<A, I, J, R>(
        Expression<Func<A, I>> root,
        Expression<Func<J, R>> branch
    )
        where I : J?
        where J : class?
        where R : class? =>
        GraftNullableImpl<Func<A, R?>>(root, branch);

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
        where I : J?
        where J : class?
        where R : class? =>
        GraftNullableImpl<Func<A, B, R?>>(root, branch);

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
        where I : J?
        where J : class?
        where R : class? =>
        GraftNullableImpl<Func<A, B, C, R?>>(root, branch);

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
        where I : J?
        where J : class?
        where R : class? =>
        GraftNullableImpl<Func<A, B, C, D, R?>>(root, branch);

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
        where I : J?
        where J : class?
        where R : struct =>
        GraftNullableImpl<Func<A, Nullable<R>>>(root, branch);

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
        where I : J?
        where J : class?
        where R : struct =>
        GraftNullableImpl<Func<A, B, Nullable<R>>>(root, branch);

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
        where I : J?
        where J : class?
        where R : struct =>
        GraftNullableImpl<Func<A, B, C, Nullable<R>>>(root, branch);

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
        where I : J?
        where J : class?
        where R : struct =>
        GraftNullableImpl<Func<A, B, C, D, Nullable<R>>>(root, branch);

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
        where I : J?
        where J : class?
        where R : struct =>
        GraftNullableImpl<Func<A, Nullable<R>>>(root, branch);

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
        where I : J?
        where J : class?
        where R : struct =>
        GraftNullableImpl<Func<A, B, Nullable<R>>>(root, branch);

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
        where I : J?
        where J : class?
        where R : struct =>
        GraftNullableImpl<Func<A, B, C, Nullable<R>>>(root, branch);

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
        where I : J?
        where J : class?
        where R : struct =>
        GraftNullableImpl<Func<A, B, C, D, Nullable<R>>>(root, branch);

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
        GraftNullableImpl<Func<A, R?>>(root, branch);

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
        GraftNullableImpl<Func<A, B, R?>>(root, branch);

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
        GraftNullableImpl<Func<A, B, C, R?>>(root, branch);

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
        GraftNullableImpl<Func<A, B, C, D, R?>>(root, branch);

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
        GraftNullableImpl<Func<A, Nullable<R>>>(root, branch);

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
        GraftNullableImpl<Func<A, B, Nullable<R>>>(root, branch);

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
        GraftNullableImpl<Func<A, B, C, Nullable<R>>>(root, branch);

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
        GraftNullableImpl<Func<A, B, C, D, Nullable<R>>>(root, branch);

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
        GraftNullableImpl<Func<A, Nullable<R>>>(root, branch);

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
        GraftNullableImpl<Func<A, B, Nullable<R>>>(root, branch);

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
        GraftNullableImpl<Func<A, B, C, Nullable<R>>>(root, branch);

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
        GraftNullableImpl<Func<A, B, C, D, Nullable<R>>>(root, branch);

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

    internal static Expression<TDelegate> GraftNullableImpl<TDelegate>(
        LambdaExpression root,
        LambdaExpression branch
    )
        where TDelegate : Delegate
    {
        AssertFuncType(typeof(TDelegate));

        var rootResultType = root.Type.GenericTypeArguments[^1];
        var branchInputType = branch.Type.GenericTypeArguments[0];

        // Calculate the result type of the expression. Note that this is based on the type of the
        // branch expression to avoid introducing explicit casts which would otherwise be handled
        // by the expression type!
        var resultType = (branch.Body.Type.IsValueType && !IsNullableType(branch.Body.Type)) switch {
            true => typeof(System.Nullable<>).MakeGenericType(branch.Body.Type),
            false => branch.Body.Type
        };

        return Expression.Lambda<TDelegate>(
            body: Expression.Condition(
                Expression.Equal(
                    root.Body,
                    Expression.Constant(null, root.Body.Type.IsValueType switch {
                        true => root.Body.Type,
                        false => typeof(object)
                    })
                ),
                Expression.Constant(null, resultType),
                // Coerce struct branch result to Nullable<T>
                Coerce(resultType, Replace(
                    branch.Body,
                    branch.Parameters[0],
                    // Coerce subtype to supertype or interface
                    Coerce(branchInputType, IsNullableType(rootResultType) switch {
                        true => Expression.Property(root.Body, rootResultType.GetProperty("Value")!),
                        false => root.Body
                    })
                ))
            ),
            parameters: root.Parameters
        );

        static bool IsNullableType(Type type) =>
            System.Nullable.GetUnderlyingType(type) is not null;

        static Expression Coerce(Type type, Expression expression) =>
            type == expression.Type ? expression : Expression.Convert(expression, type);
    }
}
