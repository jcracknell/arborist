namespace Arborist.Interpolation.Internal;

internal abstract class BaseInterpolatedExpressionVisitor : ExpressionVisitor {
    protected override Expression VisitMethodCall(MethodCallExpression node) =>
        typeof(SplicingOperations) == node.Method.DeclaringType
        ? VisitSplicingMethodCall(node)
        : base.VisitMethodCall(node);

    protected abstract Expression VisitSplicingMethodCall(MethodCallExpression node);
}
