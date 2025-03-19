namespace Arborist.Internal;

internal static class FunctionHelpers {
    /// <summary>
    /// The identity function. Returns its input value verbatim.
    /// </summary>
    public static A Identity<A>(A value) =>
        value;

    /// <summary>
    /// Immediately executes the provided <paramref name="thunk"/>, returning its result.
    /// </summary>
    public static A Immediate<A>(Func<A> thunk) =>
        thunk();
}
