using Arborist.Interpolation.Internal;

namespace Arborist.Interpolation;

/// <summary>
/// Thrown during expression interpolation in the event that an exception occurs while attempting
/// to evaluate an argument expression passed to a parameter of an <see cref="IInterpolationContext"/>
/// splicing method marked with <see cref="EvaluatedSpliceParameterAttribute"/>.
/// </summary>
public class SpliceArgumentEvaluationException : InterpolationException {
    public SpliceArgumentEvaluationException(Exception innerException)
        : base(
            message: "An exception occurred while attempting to evaluate a splice argument expression.",
            innerException: innerException
        )
    { }

    public SpliceArgumentEvaluationException(Expression expression, Exception innerException)
        : base(
            message: $"An exception occurred while attempting to evaluate splice argument expression `{expression}`.",
            innerException: innerException
        )
    { }
}
