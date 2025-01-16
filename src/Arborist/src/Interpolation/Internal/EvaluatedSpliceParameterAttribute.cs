namespace Arborist.Interpolation.Internal;

/// <summary>
/// Marks the annotated parameter to be evaluated (instead of interpolated) during the
/// expression interpolation process.
/// </summary>
/// <seealso cref="InterpolatedSpliceParameterAttribute"/>
public sealed class EvaluatedSpliceParameterAttribute : SpliceParameterAttribute { }
