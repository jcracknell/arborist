using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Arborist;

/// <typeparam name="A">
/// The type of the expression parameter.
/// </typeparam>
public static class ExpressionOn<A> {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<A, R>> Of<R>(Expression<Func<A, R>> expression) =>
        expression;

    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructor<R>(Expression<Func<A, R>> expression) =>
        ExpressionHelpers.GetConstructor(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethod<R>(Expression<Func<A, R>> expression) =>
        ExpressionHelpers.GetMethod(expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on <see cref="EI"/> with the corresponding subexpressions.
    /// </summary>
    /// <seealso cref="EI"/>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<A, R>> Interpolate<R>(Expression<Func<A, R>> expression) =>
        ExpressionHelpers.Interpolate(expression);

    /// <summary>
    /// Rebases the provided <paramref name="branch"/> expression onto the <paramref name="root"/> expression,
    /// replacing references to its parameter with the body of the <paramref name="root"/> expression.
    /// </summary>
    public static Expression<Func<A, R>> Rebase<B, R>(Expression<Func<A, B>> root, Expression<Func<B, R>> branch) =>
        Expression.Lambda<Func<A, R>>(
            body: ExpressionHelpers.Replace(branch.Body, branch.Parameters[0], root.Body),
            parameters: root.Parameters
        );

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructor<R>(
        Expression<Func<A, R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelpers.TryGetConstructor(expression, out constructorInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethod<R>(
        Expression<Func<A, R>> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelpers.TryGetMethod(expression, out methodInfo);
}
