namespace Arborist.Interpolation;

/// <summary>
/// Context for the expression interpolation process. Provides access to subtree splicing methods,
/// and data to be used in the resulting expression.
/// </summary>
public interface IInterpolationContext {
    /// <summary>
    /// Splices the provided lambda <paramref name="expression"/> into the parent expression tree
    /// as a quoted (inline) expression tree.
    /// </summary>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public TLambda SpliceQuoted<TLambda>([EvaluatedSpliceParameter] TLambda expression)
        where TLambda : LambdaExpression =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => SpliceQuoted(expression)));

    /// <summary>
    /// Splices the provided expression tree with type <typeparamref name="A"/> into the
    /// parent expression tree.
    /// </summary>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public A Splice<A>([EvaluatedSpliceParameter] Expression expression) =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => Splice<A>(expression)));

    /// <summary>
    /// Splices the delegate defined by the argument <paramref name="expression"/> into
    /// the parent expression tree.
    /// </summary>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public TDelegate Splice<TDelegate>([EvaluatedSpliceParameter] Expression<TDelegate> expression)
        where TDelegate : Delegate =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => Splice(expression)));

    /// <summary>
    /// Splices the body of the argument <paramref name="expression"/> into the
    /// parent expression tree.
    /// </summary>
    /// <typeparam name="R">
    /// The return type of the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<R>([EvaluatedSpliceParameter] Expression<Func<R>> expression) =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => SpliceBody(expression)));

    /// <summary>
    /// Splices the body of the argument <paramref name="expression"/> into the
    /// parent expression tree, replacing its arguments with the provided argument
    /// expressions.
    /// </summary>
    /// <typeparam name="A">
    /// The type of the parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="R">
    /// The return type of the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<A, R>(
        [InterpolatedSpliceParameter] A a,
        [EvaluatedSpliceParameter] Expression<Func<A, R>> expression
    ) =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => SpliceBody(a, expression)));

    /// <summary>
    /// Splices the body of the argument <paramref name="expression"/> into the
    /// parent expression tree, replacing its arguments with the provided argument
    /// expressions.
    /// </summary>
    /// <typeparam name="A">
    /// The type of the first parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="B">
    /// The type of the second parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="R">
    /// The return type of the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<A, B, R>(
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [EvaluatedSpliceParameter] Expression<Func<A, B, R>> expression
    ) =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => SpliceBody(a, b, expression)));

    /// <summary>
    /// Splices the body of the argument <paramref name="expression"/> into the
    /// parent expression tree, replacing its arguments with the provided argument
    /// expressions.
    /// </summary>
    /// <typeparam name="A">
    /// The type of the first parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="B">
    /// The type of the second parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="C">
    /// The type of the third parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="R">
    /// The return type of the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<A, B, C, R>(
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [InterpolatedSpliceParameter] C c,
        [EvaluatedSpliceParameter] Expression<Func<A, B, C, R>> expression
    ) =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => SpliceBody(a, b, c, expression)));

    /// <summary>
    /// Splices the body of the argument <paramref name="expression"/> into the
    /// parent expression tree, replacing its arguments with the provided argument
    /// expressions.
    /// </summary>
    /// <typeparam name="A">
    /// The type of the first parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="B">
    /// The type of the second parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="C">
    /// The type of the third parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="D">
    /// The type of the fourth parameter to the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <typeparam name="R">
    /// The return type of the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<A, B, C, D, R>(
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [InterpolatedSpliceParameter] C c,
        [InterpolatedSpliceParameter] D d,
        [EvaluatedSpliceParameter] Expression<Func<A, B, C, R>> expression
    ) =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => SpliceBody(a, b, c, d, expression)));

    /// <summary>
    /// Splices a constant reference to the result of the provided <paramref name="value"/>
    /// expression into the parent expression tree.
    /// </summary>
    /// <exception cref="InterpolationContextEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public A SpliceValue<A>([EvaluatedSpliceParameter] A value) =>
        throw InterpolationContextEvaluationException.Evaluated(ExpressionOnNone.GetMethod(() => SpliceValue(value)));
}
