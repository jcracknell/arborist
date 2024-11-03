using System.Reflection;

namespace Arborist.Interpolation.Internal;

public class AnalyzingInterpolationVisitor : InterpolationVisitor {
    private readonly LambdaExpression _interpolatedExpression;
    private ImmutableHashSet<ParameterExpression> _forbiddenParameters;
    private HashSet<MemberExpression>? _dataReferences;
    private HashSet<Expression>? _evaluatedExpressions;
    private Expression? _evaluatingExpression;

    public AnalyzingInterpolationVisitor(LambdaExpression interpolatedExpression) {
        _interpolatedExpression = interpolatedExpression;
        _forbiddenParameters = ImmutableHashSet.CreateRange(interpolatedExpression.Parameters);
    }

    public IReadOnlySet<Expression> EvaluatedExpressions =>
        _evaluatedExpressions ?? (IReadOnlySet<Expression>)ImmutableHashSet<Expression>.Empty;

    public IReadOnlySet<MemberExpression> DataReferences =>
        _dataReferences ?? (IReadOnlySet<MemberExpression>)ImmutableHashSet<MemberExpression>.Empty;

    protected override Expression VisitLambda<T>(Expression<T> node) {
        var snapshot = _forbiddenParameters;
        try {
            if(_evaluatingExpression is null) {
                // If we are outside of an evaluated expression tree, add newly declared parameters
                // to the set of forbidden parameters
                _forbiddenParameters = _forbiddenParameters.Union(node.Parameters);
            } else {
                // If we are in an evaluated expression and the lambda declares new parameters, they are
                // now allowed to be used (note parameters can technically be shared between expression trees)
                _forbiddenParameters = _forbiddenParameters.Except(node.Parameters);
            }

            base.Visit(node.Body);
            return node;
        } finally {
            _forbiddenParameters = snapshot;
        }
    }

    protected override Expression VisitMember(MemberExpression node) {
        if(
            node is { Expression: ParameterExpression parameter, Member: PropertyInfo property }
            && parameter.Type.IsAssignableTo(typeof(IInterpolationContext))
            && property.Name.Equals(nameof(IInterpolationContext<object>.Data))
        ) {
            (_dataReferences ??= new()).Add(node);
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitParameter(ParameterExpression node) {
        // If we are inside an evaluated argument expression, and we've encountered one of the parent
        // expression tree's parameters, that's going to be an error.
        if(_evaluatingExpression is not null && _forbiddenParameters.Contains(node))
            throw new InterpolatedParameterCaptureException(node, _evaluatingExpression);

        return node;
    }

    protected override Expression VisitSplicingMethodCall(MethodCallExpression node) {
        if(_evaluatingExpression is not null)
            throw new InterpolationContextEvaluationException(node.Method, _evaluatingExpression);

        Visit(node.Object);

        foreach(var (parameter, argumentExpression) in node.Method.GetParameters().Zip(node.Arguments)) {
            if(parameter.IsDefined(typeof(EvaluatedSpliceParameterAttribute), false)) {
                (_evaluatedExpressions ??= new()).Add(argumentExpression);

                try {
                    _evaluatingExpression = argumentExpression;
                    Visit(argumentExpression);
                } finally {
                    _evaluatingExpression = null;
                }
            } else {
                if(!parameter.IsDefined(typeof(InterpolatedSpliceParameterAttribute), false))
                    throw new Exception($"Parameter {parameter} to method {node.Method} must be annotated with one of {typeof(EvaluatedSpliceParameterAttribute)} or {typeof(InterpolatedSpliceParameterAttribute)}.");

                Visit(argumentExpression);
            }
        }

        return node;
    }
}
