namespace Arborist.Fixtures;

public readonly struct ImplicitlyConvertible<T>(T value) {
    public static implicit operator ImplicitlyConvertible<T>(T value) =>
        new(value);

    public T Value { get; } = value;
}
