namespace Arborist.Interpolation;

/// <summary>
/// Marks the annotated parameter to be evaluated (instead of interpolated) during the
/// expression interpolation process.
/// </summary>
/// <seealso cref="InterpolatedSpliceParameterAttribute"/>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EvaluatedSpliceParameterAttribute : Attribute { }
