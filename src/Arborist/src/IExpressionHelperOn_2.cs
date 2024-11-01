namespace Arborist;

/// <summary>
/// Mountpoint for expression helper extension methods operating on expressions accepting
/// two parameters.
/// </summary>
/// <typeparam name="A">
/// The type of the first expression parameter.
/// </typeparam>
/// <typeparam name="B">
/// The type of the second expression parameter.
/// </typeparam>
public interface IExpressionHelperOn<in A, in B> { }
