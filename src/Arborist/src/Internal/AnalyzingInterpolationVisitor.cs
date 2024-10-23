using Arborist.Interpolation;
using System.Collections.Immutable;

namespace Arborist.Internal;

public class AnalyzingInterpolationVisitor : InterpolationVisitor {
    private readonly LambdaExpression _interpolatedExpression;
    private ImmutableHashSet<ParameterExpression> _forbiddenParameters;
    private HashSet<Expression>? _evaluatedExpressions;
    private Expression? _evaluatedExpression;

    public AnalyzingInterpolationVisitor(LambdaExpression interpolatedExpression) {
        _interpolatedExpression = interpolatedExpression;
        _forbiddenParameters = ImmutableHashSet.CreateRange(interpolatedExpression.Parameters);
    }

    public IReadOnlySet<Expression> EvaluatedExpressions =>
        _evaluatedExpressions ?? (IReadOnlySet<Expression>)ImmutableHashSet<Expression>.Empty;

    protected override Expression VisitLambda<T>(Expression<T> node) {
        var snapshot = _forbiddenParameters;
        try {
            if(_evaluatedExpression is null) {
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

    protected override Expression VisitParameter(ParameterExpression node) {
        // If we are inside an evaluated argument expression, and we've encountered one of the parent
        // expression tree's parameters, that's going to be an error.
        if(_evaluatedExpression is not null && _forbiddenParameters.Contains(node))
            throw new InterpolatedParameterCaptureException(node, _evaluatedExpression);

        return node;
    }

    protected override Expression VisitEIMethodCall(MethodCallExpression node) {
        if(_evaluatedExpression is not null)
            throw new InterpolatedSpliceEvaluationException(node.Method, _evaluatedExpression);

        foreach(var (parameter, argumentExpression) in node.Method.GetParameters().Zip(node.Arguments)) {
            if(parameter.IsDefined(typeof(EvaluatedParameterAttribute), false)) {
                (_evaluatedExpressions ??= new()).Add(argumentExpression);

                try {
                    _evaluatedExpression = argumentExpression;
                    Visit(argumentExpression);
                } finally {
                    _evaluatedExpression = null;
                }
            } else if(!parameter.IsDefined(typeof(InterpolatedParameterAttribute), false)) {
                throw new Exception($"Parameter {parameter} to method {node.Method} must be annotated with one of {typeof(EvaluatedParameterAttribute)} or {typeof(InterpolatedParameterAttribute)}.");
            }
        }

        return node;
    }
}
