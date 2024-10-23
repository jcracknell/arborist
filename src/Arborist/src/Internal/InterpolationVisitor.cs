namespace Arborist.Internal;

public abstract class InterpolationVisitor : ExpressionVisitor {
    protected override Expression VisitMethodCall(MethodCallExpression node) =>
        typeof(EI) == node.Method.DeclaringType && node.Method.IsPublic
        ? VisitEIMethodCall(node)
        : base.VisitMethodCall(node);

    protected abstract Expression VisitEIMethodCall(MethodCallExpression node);
}
