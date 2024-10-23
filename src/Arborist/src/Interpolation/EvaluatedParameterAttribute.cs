namespace Arborist.Interpolation;

/// <summary>
/// Marks the annotated parameter to be evaluated (instead of interpolated) during the
/// expression interpolation process.
/// </summary>
/// <seealso cref="InterpolatedParameterAttribute"/>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EvaluatedParameterAttribute : Attribute { }
