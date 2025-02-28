using System.Reflection;

namespace Arborist;

public static partial class ExpressionOn<A> {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<A, R>> Of<R>(Expression<Func<A, R>> expression) =>
        expression;

    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action<A>> Of(Expression<Action<A>> expression) =>
        expression;

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<A, T>> As<T>(LambdaExpression expression) =>
        ExpressionHelper.AsCore<Func<A, T>>(typeof(T), expression);

    /// <summary>
    /// Creates a constant-valued expression with the provided <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The result type of the constant-valued expression.
    /// </typeparam>
    public static Expression<Func<A, T>> Constant<T>(T value) =>
        ExpressionHelper.Const<Func<A, T>>(default, value);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    public static Expression<Func<A, T>> Convert<T>(LambdaExpression expression) =>
        ExpressionHelper.ConvertCore<Func<A, T>>(typeof(T), expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked"/> node (or
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> if there is no defined checked
    /// conversion) of the form <c>(T)body</c>.
    /// </summary>
    public static Expression<Func<A, T>> ConvertChecked<T>(LambdaExpression expression) =>
        ExpressionHelper.ConvertCheckedCore<Func<A, T>>(typeof(T), expression);

    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructorInfo<R>(Expression<Func<A, R>> expression) =>
        ExpressionHelper.GetConstructorInfo(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo<R>(Expression<Func<A, R>> expression) =>
        ExpressionHelper.GetMethodInfo(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo(Expression<Action<A>> expression) =>
        ExpressionHelper.GetMethodInfo(expression);


    /// <summary>
    /// The identity expression: <c>a => a</c>.
    /// </summary>
    public static Expression<Func<A, A>> Identity { get; } = Of(a => a);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> so that its declared result type is nullable.
    /// </summary>
    public static Expression<Func<A, R?>> Nullable<R>(
        Expression<Func<A, R>> expression
    )
        where R : class =>
        ExpressionHelper.Nullable(expression);

    /// <summary>
    /// Wraps the provided <paramref name="expression"/> to produce a <see cref="System.Nullable{T}"/>
    /// result value.
    /// </summary>
    /// <param name="dummy">
    /// Disambiguates overloads by type parameter constraints.
    /// </param>
    public static Expression<Func<A, Nullable<R>>> Nullable<R>(
        Expression<Func<A, R>> expression,
        Nullable<R> dummy = default
    )
        where R : struct =>
        ExpressionHelper.Nullable(expression);

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    /// <remarks>
    /// This method is an escape hatch providing explicit access to the runtime interpolation
    /// implementation, and can be used as a workaround for potential bugs or deficiencies in
    /// the compile time interpolator.
    /// </remarks>
    public static bool TryGetConstructorInfo<R>(
        Expression<Func<A, R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelper.TryGetConstructorInfo(expression, out constructorInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo<R>(
        Expression<Func<A, R>> expression,
        [MaybeNullWhen(false)] out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethodInfo(expression, out methodInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo(
        Expression<Action<A>> expression,
        [MaybeNullWhen(false)] out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethodInfo(expression, out methodInfo);
}
