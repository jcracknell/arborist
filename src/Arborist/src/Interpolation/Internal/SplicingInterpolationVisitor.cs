namespace Arborist.Interpolation.Internal;

public class SplicingInterpolationVisitor : InterpolationVisitor {
    private readonly IReadOnlyDictionary<Expression, object?> _evaluatedSpliceParameters;

    public SplicingInterpolationVisitor(IReadOnlyDictionary<Expression, object?> evaluatedSpliceParameters) {
        _evaluatedSpliceParameters = evaluatedSpliceParameters;
    }

    protected override Expression VisitSplicingMethodCall(MethodCallExpression node) {
        return node.Method.Name switch {
            nameof(IInterpolationContext.Splice) => VisitSplice(node),
            nameof(IInterpolationContext.SpliceBody) => VisitSpliceBody(node),
            nameof(IInterpolationContext.Value) => VisitSpliceValue(node),
            nameof(IInterpolationContext.Quote) => VisitSpliceQuoted(node),
            _ => throw new Exception($"Unhandled {typeof(IInterpolationContext)} method: {node.Method}.")
        };
    }

    protected Expression VisitSplice(MethodCallExpression node) {
        var resultType = node.Method.GetGenericArguments()[0];
        var expressionReference = node.Arguments[0];
        var interpolatedValue = (Expression)_evaluatedSpliceParameters[expressionReference]!;

        return Coerce(resultType, interpolatedValue);
    }

    protected Expression VisitSpliceBody(MethodCallExpression node) {
        var declaredType = node.Method.GetGenericArguments()[^1];
        var expressionReference = node.Arguments[^1];
        var interpolatedLambda = (LambdaExpression)_evaluatedSpliceParameters[expressionReference]!;

        // Apply interpolation to the argument replacement expressions
        // N.B. Visit has NotNullIfNotNullAttribute
        var interpolatedArguments = node.Arguments.SkipLast(1).Select(Visit);

        var argumentReplacements = interpolatedLambda.Parameters.Zip(interpolatedArguments).ToDictionary(
            tup => (Expression)tup.First,
            tup => tup.Second!
        );

        return Coerce(declaredType, ExpressionHelper.Replace(interpolatedLambda.Body, argumentReplacements));
    }

    protected Expression VisitSpliceValue(MethodCallExpression node) {
        var declaredType = node.Method.GetGenericArguments()[0];
        var expressionReference = node.Arguments[0];
        var interpolatedValue = _evaluatedSpliceParameters[expressionReference];

        return Coerce(declaredType, Expression.Constant(interpolatedValue));
    }

    protected Expression VisitSpliceQuoted(MethodCallExpression node) {
        var expressionReference = node.Arguments[0];
        var tree = (Expression)_evaluatedSpliceParameters[expressionReference]!;

        return Expression.Quote(tree);
    }

    private Expression Coerce(Type type, Expression expression) =>
        expression.Type == type ? expression : Expression.Convert(expression, type);
}
