namespace Arborist.Interpolation.Internal;

public abstract class InterpolationVisitor : ExpressionVisitor {
    protected override Expression VisitMethodCall(MethodCallExpression node) =>
        typeof(InterpolationSpliceOperations) == node.Method.DeclaringType
        ? VisitSplicingMethodCall(node)
        : base.VisitMethodCall(node);

    protected abstract Expression VisitSplicingMethodCall(MethodCallExpression node);
}
