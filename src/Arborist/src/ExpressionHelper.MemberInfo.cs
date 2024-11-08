using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Gets the constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static ConstructorInfo GetConstructorInfo(Expression expression) =>
        TryGetConstructorInfo(expression, out var constructorInfo) switch {
            true => constructorInfo,
            false => throw new ArgumentException("Argument expression is not a simple constructor invocation.", nameof(expression))
        };

    /// <summary>
    /// Gets the method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static MethodInfo GetMethodInfo(Expression expression) =>
        TryGetMethodInfo(expression, out var methodInfo) switch {
            true => methodInfo,
            false => throw new ArgumentException("Argument expression is not a simple method call.", nameof(expression))
        };

    /// <summary>
    /// Attempts to get a constructor identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetConstructorInfo(
        Expression expression,
        [MaybeNullWhen(false)] out ConstructorInfo constructorInfo
    ) {
        if(TryGetMemberInfo(expression, out var member)) {
            constructorInfo = member as ConstructorInfo;
            return constructorInfo is not null;
        } else {
            constructorInfo = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get a method identified by the provided <paramref name="expression"/>.
    /// </summary>
    public static bool TryGetMethodInfo(
        Expression expression,
        [MaybeNullWhen(false)]out MethodInfo methodInfo
    ) {
        if(TryGetMemberInfo(expression, out var member)) {
            methodInfo = member as MethodInfo;
            return methodInfo is not null;
        } else {
            methodInfo = default;
            return false;
        }
    }

    private static bool TryGetMemberInfo(
        Expression expression,
        [MaybeNullWhen(false)] out MemberInfo member
    ) {
        switch(expression) {
            case MemberExpression mem:
                member = mem.Member;
                return true;
            case MethodCallExpression call:
                member = call.Method;
                return true;
            case IndexExpression { Indexer: not null } index:
                member = index.Indexer;
                return true;
            case NewExpression { Constructor: not null } newExpr:
                member = newExpr.Constructor;
                return true;
            case LambdaExpression lambda:
                return TryGetMemberInfo(lambda.Body, out member);
            case UnaryExpression { NodeType: ExpressionType.Convert } convert:
                return TryGetMemberInfo(convert.Operand, out member);
            default:
                member = default;
                return false;
        }
    }
}
