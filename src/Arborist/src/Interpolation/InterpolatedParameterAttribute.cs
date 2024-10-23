namespace Arborist.Interpolation;

/// <summary>
/// Marks the annotated parameter to be interpolated (instead of evaluated) during the
/// expression interpolation process.
/// </summary>
/// <seealso cref="EvaluatedParameterAttribute"/>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class InterpolatedParameterAttribute : Attribute { }
