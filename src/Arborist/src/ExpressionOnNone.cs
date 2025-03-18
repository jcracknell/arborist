using Arborist.Utils;
using System.Reflection;

namespace Arborist;

public static partial class ExpressionOnNone {
    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Func<R>> Of<R>(Expression<Func<R>> expression) =>
        expression;

    /// <summary>
    /// Returns the provided <paramref name="expression"/> verbatim.
    /// </summary>
    /// <remarks>
    /// This method provides assistance with type inferral when constructing expressions.
    /// </remarks>
    public static Expression<Action> Of(Expression<Action> expression) =>
        expression;

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.TypeAs"/> node of the form
    /// <c>body as T</c>.
    /// </summary>
    public static Expression<Func<T>> As<T>(LambdaExpression expression) =>
        ExpressionHelper.AsCore<Func<T>>(typeof(T), expression);

    /// <summary>
    /// Creates a constant-valued expression with the provided <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The result type of the constant-valued expression.
    /// </typeparam>
    public static Expression<Func<T>> Constant<T>(T value) =>
        ExpressionHelper.Const<Func<T>>(default, value);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> node of the form
    /// <c>(T)body</c>.
    /// </summary>
    public static Expression<Func<T>> Convert<T>(LambdaExpression expression) =>
        ExpressionHelper.ConvertCore<Func<T>>(typeof(T), expression);

    /// <summary>
    /// Wraps the body of the provided lambda <paramref name="expression"/> in a
    /// <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked"/> node (or
    /// <see cref="System.Linq.Expressions.ExpressionType.Convert"/> if there is no defined checked
    /// conversion) of the form <c>(T)body</c>.
    /// </summary>
    public static Expression<Func<T>> ConvertChecked<T>(LambdaExpression expression) =>
        ExpressionHelper.ConvertCheckedCore<Func<T>>(typeof(T), expression);

    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructorInfo<R>(
        Expression<Func<R>> expression
    ) =>
        ExpressionHelper.GetConstructorInfo(expression);


    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo<R>(
        Expression<Func<R>> expression
    ) =>
        ExpressionHelper.GetMethodInfo(expression);

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo(
        Expression<Action> expression
    ) =>
        ExpressionHelper.GetMethodInfo(expression);

    /// <summary>
    /// Converts the provided <paramref name="expression"/> so that its declared result type is nullable.
    /// </summary>
    public static Expression<Func<R?>> Nullable<R>(
        Expression<Func<R>> expression
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
    public static Expression<Func<Nullable<R>>> Nullable<R>(
        Expression<Func<R>> expression,
        Dummy dummy = default
    )
        where R : struct =>
        ExpressionHelper.Nullable(expression);

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructorInfo<R>(
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) =>
        ExpressionHelper.TryGetConstructorInfo(expression, out constructorInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo<R>(
        Expression<Func<R>> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethodInfo(expression, out methodInfo);

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo(
        Expression<Action> expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) =>
        ExpressionHelper.TryGetMethodInfo(expression, out methodInfo);
}
