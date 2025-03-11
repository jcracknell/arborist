using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

/// <summary>
/// Provides extension methods defining the splicing operations accessible via the
/// <see cref="IInterpolationContext"/> during expression interpolation.
/// </summary>
public static class InterpolationSpliceOperations {
    /// <summary>
    /// Splices the provided expression tree with type <typeparamref name="A"/> into the
    /// parent expression tree.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static A Splice<A>(
        this IInterpolationContext context,
        [EvaluatedSpliceParameter] Expression expression
    ) =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceInvoked);

    /// <summary>
    /// Splices the delegate defined by the argument <paramref name="expression"/> into
    /// the parent expression tree.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static TDelegate Splice<TDelegate>(
        this IInterpolationContext context,
        [EvaluatedSpliceParameter] Expression<TDelegate> expression
    )
        where TDelegate : Delegate =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceInvoked);

    /// <summary>
    /// Splices the body of the argument <paramref name="expression"/> into the
    /// parent expression tree.
    /// </summary>
    /// <typeparam name="R">
    /// The return type of the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<R>(
        this IInterpolationContext context,
        [EvaluatedSpliceParameter] Expression<Func<R>> expression
    ) =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceBodyInvoked);

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
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<A, R>(
        this IInterpolationContext context,
        [InterpolatedSpliceParameter] A a,
        [EvaluatedSpliceParameter] Expression<Func<A, R>> expression
    ) =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceBodyInvoked);

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
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<A, B, R>(
        this IInterpolationContext context,
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [EvaluatedSpliceParameter] Expression<Func<A, B, R>> expression
    ) =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceBodyInvoked);

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
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<A, B, C, R>(
        this IInterpolationContext context,
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [InterpolatedSpliceParameter] C c,
        [EvaluatedSpliceParameter] Expression<Func<A, B, C, R>> expression
    ) =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceBodyInvoked);

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
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<A, B, C, D, R>(
        this IInterpolationContext context,
        [InterpolatedSpliceParameter] A a,
        [InterpolatedSpliceParameter] B b,
        [InterpolatedSpliceParameter] C c,
        [InterpolatedSpliceParameter] D d,
        [EvaluatedSpliceParameter] Expression<Func<A, B, C, R>> expression
    ) =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceBodyInvoked);

    /// <summary>
    /// Splices a constant value or constant reference to the result of the provided <paramref name="value"/>
    /// expression into the parent expression tree.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static A SpliceConstant<A>(
        this IInterpolationContext context,
        [EvaluatedSpliceParameter] A value
    ) =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceConstantInvoked);

    /// <summary>
    /// Splices the provided lambda <paramref name="expression"/> into the parent expression tree
    /// as a quoted (inline) expression tree.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static TLambda SpliceQuoted<TLambda>(
        this IInterpolationContext context,
        [EvaluatedSpliceParameter] TLambda expression
    )
        where TLambda : LambdaExpression =>
        throw new InvalidOperationException(InterpolationStrings.InterpolationContextSpliceQuotedInvoked);
}
