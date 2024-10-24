using Arborist.Interpolation;
using System.Reflection;

namespace Arborist;

// This class is named thusly because the C# compiler cannot infer the result type of
// a "curried" style lambda of the form `ei => x => x` without carrying the parameter types
// in the interpolator and forcing result type inferral via a method call.
// This design has precedent in EntityFramework (the EF class), and allows us to retain
// ExpressionHelpers.Interpolate<TDelegate> as a reasonably simple base interpolation
// entrypoint.

/// <summary>
/// The expression interpolation splicer. Provides methods which mark subexpressions to be spliced
/// into the parent expression tree by the interpolation process.
/// </summary>
/// <seealso cref="ExpressionHelpers.Interpolate{TDelegate}"/>
public static class EI {
    private static InterpolatedSpliceEvaluationException Evaluated(MethodInfo methodInfo) =>
        new($"Expression splicing method {methodInfo} should only be used in an interpolated expression.");

    /// <summary>
    /// Splices the provided <paramref name="expression"/> into the parent expression tree
    /// as a quoted (inline) expression tree.
    /// </summary>
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static TExpression Quote<TExpression>([EvaluatedParameter] TExpression expression)
        where TExpression : Expression =>
        throw Evaluated(ExpressionThunk.GetMethod(() => Quote(expression)));

    /// <summary>
    /// Splices the provided expression tree with type <typeparamref name="A"/> into the
    /// parent expression tree.
    /// </summary>
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static A Splice<A>([EvaluatedParameter] Expression expression) =>
        throw Evaluated(ExpressionThunk.GetMethod(() => Splice<A>(expression)));

    /// <summary>
    /// Splices the delegate defined by the argument <paramref name="expression"/> into
    /// the parent expression tree.
    /// </summary>
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static TDelegate Splice<TDelegate>([EvaluatedParameter] Expression<TDelegate> expression)
        where TDelegate : Delegate =>
        throw Evaluated(ExpressionThunk.GetMethod(() => Splice(expression)));

    /// <summary>
    /// Splices the body of the argument <paramref name="expression"/> into the
    /// parent expression tree.
    /// </summary>
    /// <typeparam name="R">
    /// The return type of the interpolated <paramref name="expression"/>.
    /// </typeparam>
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<R>([EvaluatedParameter] Expression<Func<R>> expression) =>
        throw Evaluated(ExpressionThunk.GetMethod(() => SpliceBody(expression)));

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
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<A, R>(
        [InterpolatedParameter] A a,
        [EvaluatedParameter] Expression<Func<A, R>> expression
    ) =>
        throw Evaluated(ExpressionThunk.GetMethod(() => SpliceBody(a, expression)));

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
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<A, B, R>(
        [InterpolatedParameter] A a,
        [InterpolatedParameter] B b,
        [EvaluatedParameter] Expression<Func<A, B, R>> expression
    ) =>
        throw Evaluated(ExpressionThunk.GetMethod(() => SpliceBody(a, b, expression)));

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
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<A, B, C, R>(
        [InterpolatedParameter] A a,
        [InterpolatedParameter] B b,
        [InterpolatedParameter] C c,
        [EvaluatedParameter] Expression<Func<A, B, C, R>> expression
    ) =>
        throw Evaluated(ExpressionThunk.GetMethod(() => SpliceBody(a, b, c, expression)));

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
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static R SpliceBody<A, B, C, D, R>(
        [InterpolatedParameter] A a,
        [InterpolatedParameter] B b,
        [InterpolatedParameter] C c,
        [InterpolatedParameter] D d,
        [EvaluatedParameter] Expression<Func<A, B, C, R>> expression
    ) =>
        throw Evaluated(ExpressionThunk.GetMethod(() => SpliceBody(a, b, c, d, expression)));

    /// <summary>
    /// Splices a constant reference to the result of the provided <paramref name="value"/>
    /// expression into the parent expression tree.
    /// </summary>
    /// <exception cref="InterpolatedSpliceEvaluationException">
    /// This method should only be used in an interpolated expression.
    /// </exception>
    public static A Value<A>([EvaluatedParameter] A value) =>
        throw Evaluated(ExpressionThunk.GetMethod(() => Value(value)));
}
