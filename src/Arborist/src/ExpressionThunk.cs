using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Arborist;

public static class ExpressionThunk {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<R>> Of<R>(Expression<Func<R>> expression) =>
        expression;

    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructor<R>(Expression<Func<R>> expression) =>
        ExpressionHelpers.GetConstructor(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethod<R>(Expression<Func<R>> expression) =>
        ExpressionHelpers.GetMethod(expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on <see cref="EI"/> with the corresponding subexpressions.
    /// </summary>
    /// <seealso cref="EI"/>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    public static Expression<Func<R>> Interpolate<R>(Expression<Func<R>> expression) =>
        ExpressionHelpers.Interpolate(expression);

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructor<R>(
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelpers.TryGetConstructor(expression, out constructorInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethod<R>(
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelpers.TryGetMethod(expression, out methodInfo);
}
