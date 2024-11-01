namespace Arborist;

internal sealed class ExpressionHelperOn<A> : IExpressionHelperOn<A> {
    public static ExpressionHelperOn<A> Instance { get; } = new();
    
    private ExpressionHelperOn() { }
}
