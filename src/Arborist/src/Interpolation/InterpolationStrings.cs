namespace Arborist.Interpolation;

internal static class InterpolationStrings {
    public static string InterpolationContextDataInvoked =>
        InterpolationContextInvocation(nameof(IInterpolationContext<object>.Data));

    public static string InterpolationContextSpliceInvoked =>
        InterpolationContextInvocation(nameof(InterpolationSpliceOperations.Splice));

    public static string InterpolationContextSpliceBodyInvoked =>
        InterpolationContextInvocation(nameof(InterpolationSpliceOperations.SpliceBody));

    public static string InterpolationContextSpliceConstantInvoked =>
        InterpolationContextInvocation(nameof(InterpolationSpliceOperations.SpliceConstant));

    public static string InterpolationContextSpliceQuotedInvoked =>
        InterpolationContextInvocation(nameof(InterpolationSpliceOperations.SpliceQuoted));

    private static string InterpolationContextInvocation(string memberName) =>
        $"{nameof(IInterpolationContext)}.{memberName} may only be used within an interpolated expression.";
}
