namespace Arborist;

/// <summary>
/// Mountpoint for expression helper extension methods operating on expressions accepting
/// four parameters.
/// </summary>
/// <typeparam name="A">
/// The type of the first expression parameter.
/// </typeparam>
/// <typeparam name="B">
/// The type of the second expression parameter.
/// </typeparam>
/// <typeparam name="C">
/// The type of the third expression parameter.
/// </typeparam>
/// <typeparam name="D">
/// The type of the fourth expression parameter.
/// </typeparam>
public interface IExpressionHelperOn<in A, in B, in C, in D> { }
