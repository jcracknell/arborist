using System.Reflection;

namespace Arborist.Interpolation.Internal;

internal abstract class BaseInterpolatedExpressionVisitor : ExpressionVisitor {
    private ParameterExpression? _contextParameter;

    public BaseInterpolatedExpressionVisitor(LambdaExpression interpolatedExpression) {
        _contextParameter = interpolatedExpression.Parameters[0];
    }

    protected abstract Expression VisitSplicingCall(MethodCallExpression node);

    private bool IsSplicingCall(MethodCallExpression node) {
        if(typeof(SplicingOperations) != node.Method.DeclaringType)
            return false;
        if(node.Arguments.Count == 0)
            return false;
        if(node.Arguments[0] is not ParameterExpression parameter)
            return false;
        if(!ReferenceEquals(parameter, _contextParameter))
            return false;

        return true;
    }

    protected abstract Expression VisitInterpolationData(MemberExpression node);

    private bool IsInterpolationData(MemberExpression node) {
        if(node is not { Expression: ParameterExpression parameter, Member: PropertyInfo property })
            return false;
        if(!ReferenceEquals(parameter, _contextParameter))
            return false;
        if(!property.Name.Equals(nameof(IInterpolationContext<object>.Data)))
            return false;

        return true;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node) =>
        IsSplicingCall(node) switch {
            true => VisitSplicingCall(node),
            false => base.VisitMethodCall(node)
        };

    protected override Expression VisitMember(MemberExpression node) =>
        IsInterpolationData(node) switch {
            true => VisitInterpolationData(node),
            false => base.VisitMember(node)
        };

    protected override Expression VisitLambda<T>(Expression<T> node) {
        var snapshot = _contextParameter;

        // Look for a parameter shadowing our context parameter in the body of the lambda
        for(var i = 0; i < node.Parameters.Count; i++) {
            var parameter = node.Parameters[i];
            if(parameter.Name is not null && parameter.Name.Equals(_contextParameter?.Name)) {
                _contextParameter = null;
                break;
            }
        }

        var result = base.VisitLambda(node);

        _contextParameter = snapshot;
        return result;
    }
}
