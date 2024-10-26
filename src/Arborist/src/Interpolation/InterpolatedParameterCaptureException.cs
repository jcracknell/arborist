namespace Arborist.Interpolation;

/// <summary>
/// Thrown in the event that a subtree of an interpolated expression passed to a parameter
/// annotated with <see cref="EvaluatedSpliceParameterAttribute"/> of a splicing method
/// defined on <see cref="IInterpolationContext"/> captures a parameter of the interpolated
/// expression.
/// </summary>
public class InterpolatedParameterCaptureException : Exception {
    public InterpolatedParameterCaptureException(ParameterExpression parameter, Expression evaluated)
        : base($"Interpolated parameter {parameter} is captured by evaluated expression {evaluated}.")
    { }
}
