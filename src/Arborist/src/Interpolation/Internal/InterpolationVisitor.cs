namespace Arborist.Interpolation.Internal;

public abstract class InterpolationVisitor : ExpressionVisitor {
    protected override Expression VisitMethodCall(MethodCallExpression node) =>
        typeof(IInterpolationContext) == node.Method.DeclaringType && node.Method.IsPublic
        ? VisitSplicingMethodCall(node)
        : base.VisitMethodCall(node);

    protected abstract Expression VisitSplicingMethodCall(MethodCallExpression node);
}
