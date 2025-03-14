namespace Arborist.Interpolation;

internal static class InterpolationStrings {
    public static string InterpolationContextDataInvoked =>
        InterpolationContextInvocation(nameof(IInterpolationContext<object>.Data));

    public static string InterpolationContextSpliceInvoked =>
        InterpolationContextInvocation(nameof(SplicingOperations.Splice));

    public static string InterpolationContextSpliceBodyInvoked =>
        InterpolationContextInvocation(nameof(SplicingOperations.SpliceBody));

    public static string InterpolationContextSpliceConstantInvoked =>
        InterpolationContextInvocation(nameof(SplicingOperations.SpliceConstant));

    public static string InterpolationContextSpliceQuotedInvoked =>
        InterpolationContextInvocation(nameof(SplicingOperations.SpliceQuoted));

    private static string InterpolationContextInvocation(string memberName) =>
        $"{nameof(IInterpolationContext)}.{memberName} may only be used within an interpolated expression.";
}
