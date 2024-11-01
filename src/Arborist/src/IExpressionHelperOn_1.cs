namespace Arborist;

/// <summary>
/// Mountpoint for expression helper extension methods operating on expressions accepting
/// a single parameter.
/// </summary>
/// <typeparam name="A">
/// The type of the first expression parameter.
/// </typeparam>
public interface IExpressionHelperOn<in A> { }
