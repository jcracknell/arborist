namespace Arborist.Utils;

/// <summary>
/// Static factory for <see cref="TypeOf{T}"/> values.
/// </summary>
public static class TypeOf {
    /// <summary>
    /// Creates a <see cref="TypeOf{T}"/> value for the specified type <typeparamref name="T"/>.
    /// </summary>
    public static TypeOf<T> Create<T>() => default;

    /// <summary>
    /// Creates a <see cref="TypeOf{T}"/> value for the type of the provided <paramref name="exemplar"/>
    /// value. This method can be used to create <see cref="TypeOf{T}"/> instances for anonymous classes.
    /// </summary>
    public static TypeOf<T> Create<T>(T exemplar) => default;
}

/// <summary>
/// A type-parametrized reference to type <typeparamref name="T"/>.
/// Provides a generalized way to "infer" type parameters by passing them as values.
/// </summary>
public readonly struct TypeOf<T> {
    /// <summary>
    /// The default <see cref="TypeOf{T}"/> value.
    /// </summary>
    public static TypeOf<T> Value => default;

    /// <summary>
    /// Gets the <see cref="Type"/> instance represented by this <see cref="TypeOf{T}"/>.
    /// </summary>
    public Type Type => typeof(T);

    /// <summary>
    /// Gets the default value of the type represented by this <see cref="TypeOf{T}"/>.
    /// </summary>
    public T? Default => default;

    /// <summary>
    /// Casts the provided argument value to the type represented by this <see cref="TypeOf{T}"/>.
    /// </summary>
    [return: NotNullIfNotNull("value")]
    public T? Cast(object? value) => (T?)value;

    /// <summary>
    /// Can be used to coerce an implicit conversion to the type represented by this <see cref="TypeOf{T}"/>.
    /// </summary>
    [return: NotNullIfNotNull("value")]
    public T? Coerce(T? value) => value;
}
