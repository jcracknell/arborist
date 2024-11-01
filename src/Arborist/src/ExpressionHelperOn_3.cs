namespace Arborist;

internal sealed class ExpressionHelperOn<A, B, C> : IExpressionHelperOn<A, B, C> {
    public static ExpressionHelperOn<A, B, C> Instance { get; } = new();

    private ExpressionHelperOn() { }
}
