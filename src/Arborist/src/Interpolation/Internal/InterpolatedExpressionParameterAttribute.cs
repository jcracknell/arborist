namespace Arborist.Interpolation.Internal;

/// <summary>
/// Marks the annotated parameter as an expression subjected to the interpolation process.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class InterpolatedExpressionParameterAttribute : Attribute { }
