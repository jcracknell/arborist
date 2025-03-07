namespace Arborist.Interpolation;

/// <summary>
/// Thrown in the event that the <see cref="IInterpolationContext{TData}"/> parameter provided to an
/// interpolated expression tree is referenced outside of a splicing call.
/// </summary>
public sealed class InterpolationContextReferenceException : InterpolationException {
    private InterpolationContextReferenceException(string message) : base(message) { }

    public InterpolationContextReferenceException(ParameterExpression parameter)
        : this($"Interpolated expression contains a reference to the context parameter `{parameter.Name}` which is not part of a splicing call.")
    { }
}
