using System.Reflection;

namespace Arborist.Interpolation.Internal;

public static class InterpolationInterceptorHelpers {
    /// <summary>
    /// Retrieves the value of a local variable captured by an <see cref="Expression"/> tree
    /// as a field of a display class referenced by the provided <see cref="MemberExpression"/>.
    /// </summary>
    public static object? GetCapturedLocalValue(MemberExpression expression) {
        if(expression is not { Expression: ConstantExpression { Value: var receiver }, Member: FieldInfo field })
            throw new ArgumentException(
                $"Argument {nameof(expression)} is expected to be a {nameof(MemberExpression)} referencing a field of a display class bound by a {nameof(ConstantExpression)}.",
                nameof(expression)
            );

        return field.GetValue(receiver);
    }
}
