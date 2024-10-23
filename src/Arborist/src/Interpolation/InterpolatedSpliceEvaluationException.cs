using System.Reflection;

namespace Arborist.Interpolation;

/// <summary>
/// Thrown in the event that an expression splicing method defined on <see cref="EI"/> is called at
/// runtime, or is called within a subtree of an interpolated expression passed to a splicing method
/// parameter annotated with <see cref="EvaluatedParameterAttribute"/>.
/// </summary>
public class InterpolatedSpliceEvaluationException : Exception {
    public InterpolatedSpliceEvaluationException(string message) : base(message) { }

    public InterpolatedSpliceEvaluationException(MethodInfo method, Expression expression)
        : this($"Expression splicing method {method} is called by evaluated expression {expression}.")
    { }
}
