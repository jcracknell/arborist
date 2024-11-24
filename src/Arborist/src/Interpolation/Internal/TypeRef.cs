using System.Runtime.CompilerServices;

namespace Arborist.Interpolation.Internal;

public static class TypeRef {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeRef<T> Create<T>(T exemplar) =>
        default;
}

public readonly struct TypeRef<T> {
    public static TypeRef<T> Instance => default;

    public Type Type => typeof(T);
    public T Default => default(T)!;
    public T Cast(object? value) => (T)value!;
    public T Coerce(T value) => value;
}
