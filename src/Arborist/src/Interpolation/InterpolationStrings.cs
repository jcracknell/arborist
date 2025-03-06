namespace Arborist.Interpolation;

internal static class InterpolationStrings {
    public static string InterpolationContextDataInvoked =>
        InterpolationContextInvocation(nameof(IInterpolationContext<object>.Data));

    public static string InterpolationContextSpliceInvoked =>
        InterpolationContextInvocation(nameof(IInterpolationContext.Splice));

    public static string InterpolationContextSpliceBodyInvoked =>
        InterpolationContextInvocation(nameof(IInterpolationContext.SpliceBody));

    public static string InterpolationContextSpliceConstantInvoked =>
        InterpolationContextInvocation(nameof(IInterpolationContext.SpliceConstant));

    public static string InterpolationContextSpliceQuotedInvoked =>
        InterpolationContextInvocation(nameof(IInterpolationContext.SpliceQuoted));

    private static string InterpolationContextInvocation(string memberName) =>
        $"{nameof(IInterpolationContext)}.{memberName} may only be used within an interpolated expression.";
}
