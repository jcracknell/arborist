using System.Reflection;

namespace Arborist;

public static partial class ExpressionHelperExtensions {
    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructor<R>(
        this IExpressionHelperOnNone helper,
        Expression<Func<R>> expression
    ) =>
        ExpressionHelper.GetConstructor(expression);

    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructor<A, R>(
        this IExpressionHelperOn<A> helper,
        Expression<Func<A, R>> expression
    ) =>
        ExpressionHelper.GetConstructor(expression);
        
    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethod<R>(
        this IExpressionHelperOnNone helper,
        Expression<Func<R>> expression
    ) =>
        ExpressionHelper.GetMethod(expression);
        
    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethod(
        this IExpressionHelperOnNone helper,
        Expression<Action> expression
    ) =>
        ExpressionHelper.GetMethod(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethod<A, R>(
        this IExpressionHelperOn<A> helper,
        Expression<Func<A, R>> expression
    ) =>
        ExpressionHelper.GetMethod(expression);
        
    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethod<A>(
        this IExpressionHelperOn<A> helper,
        Expression<Action<A>> expression
    ) =>
        ExpressionHelper.GetMethod(expression);

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructor<R>(
        this IExpressionHelperOnNone helper,
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelper.TryGetConstructor(expression, out constructorInfo);
        
    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructor<A, R>(
        this IExpressionHelperOn<A> helper,
        Expression<Func<A, R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelper.TryGetConstructor(expression, out constructorInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethod<R>(
        this IExpressionHelperOnNone helper,
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethod(expression, out methodInfo);
        
    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethod(
        this IExpressionHelperOnNone helper,
        Expression<Action> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethod(expression, out methodInfo);
        
    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethod<A, R>(
        this IExpressionHelperOn<A> helper,
        Expression<Func<A, R>> expression,
        [MaybeNullWhen(false)] out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethod(expression, out methodInfo);
        
    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethod<A>(
        this IExpressionHelperOn<A> helper,
        Expression<Action<A>> expression,
        [MaybeNullWhen(false)] out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethod(expression, out methodInfo);
}
