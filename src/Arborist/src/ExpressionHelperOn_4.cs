namespace Arborist;

internal sealed class ExpressionHelperOn<A, B, C, D> : IExpressionHelperOn<A, B, C, D> {
    public static ExpressionHelperOn<A, B, C, D> Instance { get; } = new();

    private ExpressionHelperOn() { }
}
