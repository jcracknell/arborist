using System.Reflection;

namespace Arborist.Interpolation;

/// <summary>
/// Thrown in the event that a member of an <see cref="IInterpolationContext"/> is evaluated at runtime,
/// or is evaluated within a subtree of an interpolated expression passed to a splicing method parameter
/// annotated with <see cref="EvaluatedSpliceParameterAttribute"/>.
/// </summary>
public class InterpolationContextEvaluationException : Exception {
    public static InterpolationContextEvaluationException Evaluated(MemberInfo member) =>
        new($"{nameof(IInterpolationContext)} member {member} should only be used within an interpolated expression.");

    public InterpolationContextEvaluationException(string message) : base(message) { }

    public InterpolationContextEvaluationException(MethodInfo method, Expression expression)
        : this($"Expression splicing method {method} is called by evaluated expression {expression}.")
    { }
}
