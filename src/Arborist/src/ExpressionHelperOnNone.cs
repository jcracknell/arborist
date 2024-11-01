namespace Arborist;

internal sealed class ExpressionHelperOnNone : IExpressionHelperOnNone {
    public static ExpressionHelperOnNone Instance { get; } = new();
    
    private ExpressionHelperOnNone() { }
}
