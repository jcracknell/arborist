namespace Arborist.Interpolation;

/// <summary>
/// Base type for exceptions related to the expression interpolation process.
/// </summary>
public class InterpolationException : Exception {
    public InterpolationException(string message)
        : base(message)
    { }

    public InterpolationException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
