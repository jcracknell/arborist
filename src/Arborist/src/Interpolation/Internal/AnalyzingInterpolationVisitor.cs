using System.Reflection;

namespace Arborist.Interpolation.Internal;

public class AnalyzingInterpolationVisitor : InterpolationVisitor {
    private readonly LambdaExpression _interpolatedExpression;
    private ImmutableHashSet<ParameterExpression> _forbiddenParameters;
    private HashSet<MemberExpression>? _dataReferences;
    private List<Expression>? _evaluatedExpressions;
    private Expression? _evaluatingExpression;

    public AnalyzingInterpolationVisitor(LambdaExpression interpolatedExpression) {
        _interpolatedExpression = interpolatedExpression;
        _forbiddenParameters = ImmutableHashSet.CreateRange(interpolatedExpression.Parameters);
    }

    // N.B. this MUST be a list, as an expression may appear multiple times in the tree (depending
    // on how it is constructed), but MUST be evaluated each time it appears, in the order that
    // they appear in order to preserve any expected side effects.
    public IReadOnlyList<Expression> EvaluatedExpressions =>
        _evaluatedExpressions ?? (IReadOnlyList<Expression>)Array.Empty<Expression>();

    public IReadOnlySet<MemberExpression> DataReferences =>
        _dataReferences ?? (IReadOnlySet<MemberExpression>)ImmutableHashSet<MemberExpression>.Empty;

    public Expression Apply(Expression expression) =>
        Visit(expression);

    protected override Expression VisitLambda<T>(Expression<T> node) {
        var snapshot = _forbiddenParameters;

        _forbiddenParameters = _evaluatingExpression switch {
            // If we are outside of an evaluated expression tree, add newly declared parameters
            // to the set of forbidden parameters
            null => _forbiddenParameters.Union(node.Parameters),
            // If we are in an evaluated expression and the lambda declares new parameters, they are
            // now allowed to be used (note parameters can technically be shared between expression trees)
            not null => _forbiddenParameters.Except(node.Parameters)
        };

        base.Visit(node.Body);

        _forbiddenParameters = snapshot;
        return node;
    }

    protected override Expression VisitMember(MemberExpression node) {
        if(
            _evaluatingExpression is not null
            && node is { Expression: not null, Member: PropertyInfo property }
            && node.Expression.Type.IsAssignableTo(typeof(IInterpolationContext))
            && property.Name.Equals(nameof(IInterpolationContext<object>.Data))
        ) {
            (_dataReferences ??= new()).Add(node);
            return node;
        } else {
            return base.VisitMember(node);
        }
    }

    protected override Expression VisitParameter(ParameterExpression node) {
        // If the context parameter appears outside of a splicing call in the interpolated region of
        // the tree, that's an error, as the context parameter does not exist in the result expression.
        if(_evaluatingExpression is null && _interpolatedExpression.Parameters[0].Equals(node))
            throw new InterpolationContextReferenceException(node);

        // You can't reference an interpolated parameter from within an evaluated tree, as they are
        // not defined during evaluation. Note that this also handles interpolation context references.
        if(_evaluatingExpression is not null && _forbiddenParameters.Contains(node))
            throw new InterpolatedParameterEvaluationException(node, _evaluatingExpression);

        return node;
    }

    protected override Expression VisitSplicingMethodCall(MethodCallExpression node) {
        // Trigger the InterpolatedParameterEvaluationException for a splice in an evaluated expression
        if(_evaluatingExpression is not null)
            Visit(node.Object);

        foreach(var (parameter, argumentExpression) in node.Method.GetParameters().Zip(node.Arguments)) {
            if(parameter.IsDefined(typeof(EvaluatedSpliceParameterAttribute), false)) {
                (_evaluatedExpressions ??= new(1)).Add(argumentExpression);

                _evaluatingExpression = argumentExpression;
                Visit(argumentExpression);
                _evaluatingExpression = null;
            } else {
                if(!parameter.IsDefined(typeof(InterpolatedSpliceParameterAttribute), false))
                    throw new Exception($"Parameter {parameter} to method {node.Method} must be annotated with one of {typeof(EvaluatedSpliceParameterAttribute)} or {typeof(InterpolatedSpliceParameterAttribute)}.");

                Visit(argumentExpression);
            }
        }

        return node;
    }
}
