namespace Arborist;

internal sealed class ExpressionHelperOn<A, B> : IExpressionHelperOn<A, B> {
    public static ExpressionHelperOn<A, B> Instance { get; } = new();

    private ExpressionHelperOn() { }
}
