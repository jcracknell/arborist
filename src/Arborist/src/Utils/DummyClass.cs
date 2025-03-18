namespace Arborist.Utils;

/// <summary>
/// Meaningless reference type used to disambiguate methods overloaded by type constraints.
/// </summary>
public sealed class DummyClass {
    private DummyClass() { }
}

/// <summary>
/// Meaningless reference type used to disambiguate methods overloaded by type constraints.
/// </summary>
public sealed class DummyClass<A> {
    private DummyClass() { }
}
