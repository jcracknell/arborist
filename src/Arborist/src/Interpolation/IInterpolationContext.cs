using Arborist.Interpolation.Internal;

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
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public TLambda SpliceQuoted<TLambda>([EvaluatedSpliceParameter] TLambda expression)
        where TLambda : LambdaExpression =>
        throw new NotImplementedException(nameof(SpliceQuoted));

    /// <summary>
    /// Splices the provided expression tree with type <typeparamref name="A"/> into the
    /// parent expression tree.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public A Splice<A>([EvaluatedSpliceParameter] Expression expression) =>
        throw new NotImplementedException(nameof(Splice));

    /// <summary>
    /// Splices the delegate defined by the argument <paramref name="expression"/> into
    /// the parent expression tree.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public TDelegate Splice<TDelegate>([EvaluatedSpliceParameter] Expression<TDelegate> expression)
        where TDelegate : Delegate =>
        throw new NotImplementedException(nameof(Splice));

    /// <summary>
    /// Splices the body of the argument <paramref name="expression"/> into the
    /// parent expression tree.
    /// </summary>
    /// <typeparam name="R">
    /// The return type of the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<R>([EvaluatedSpliceParameter] Expression<Func<R>> expression) =>
        throw new NotImplementedException(nameof(SpliceBody));

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
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<A, R>(
        [InterpolatedSpliceParameter] A a,
        [EvaluatedSpliceParameter] Expression<Func<A, R>> expression
    ) =>
        throw new NotImplementedException(nameof(SpliceBody));

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
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<A, B, R>(
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [EvaluatedSpliceParameter] Expression<Func<A, B, R>> expression
    ) =>
        throw new NotImplementedException(nameof(SpliceBody));

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
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<A, B, C, R>(
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [InterpolatedSpliceParameter] C c,
        [EvaluatedSpliceParameter] Expression<Func<A, B, C, R>> expression
    ) =>
        throw new NotImplementedException(nameof(SpliceBody));

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
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public R SpliceBody<A, B, C, D, R>(
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [InterpolatedSpliceParameter] C c,
        [InterpolatedSpliceParameter] D d,
        [EvaluatedSpliceParameter] Expression<Func<A, B, C, R>> expression
    ) =>
        throw new NotImplementedException(nameof(SpliceBody));

    /// <summary>
    /// Splices a constant value or constant reference to the result of the provided <paramref name="value"/>
    /// expression into the parent expression tree.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public A SpliceConstant<A>([EvaluatedSpliceParameter] A value) =>
        throw new NotImplementedException(nameof(SpliceConstant));
}
