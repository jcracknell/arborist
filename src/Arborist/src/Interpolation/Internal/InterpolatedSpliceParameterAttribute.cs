namespace Arborist.Interpolation.Internal;

/// <summary>
/// Marks the annotated parameter to be interpolated (instead of evaluated) during the
/// expression interpolation process.
/// </summary>
/// <seealso cref="EvaluatedSpliceParameterAttribute"/>
public sealed class InterpolatedSpliceParameterAttribute : SpliceParameterAttribute { }
