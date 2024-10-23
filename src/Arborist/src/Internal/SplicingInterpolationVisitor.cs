namespace Arborist.Internal;

public class SplicingInterpolationVisitor : InterpolationVisitor {
    private readonly IReadOnlyDictionary<Expression, object?> _evaluated;

    public SplicingInterpolationVisitor(IReadOnlyDictionary<Expression, object?> evaluated) {
        _evaluated = evaluated;
    }

    protected override Expression VisitEIMethodCall(MethodCallExpression node) {
        return node.Method.Name switch {
            nameof(EI.Splice) => VisitEISplice(node),
            nameof(EI.SpliceBody) => VisitEISpliceBody(node),
            nameof(EI.Value) => VisitEIValue(node),
            nameof(EI.Quote) => VisitEIQuote(node),
            _ => throw new Exception($"Unhandled {typeof(EI)} method: {node.Method}.")
        };
    }

    protected Expression VisitEISplice(MethodCallExpression node) {
        var resultType = node.Method.GetGenericArguments()[0];
        var expressionReference = node.Arguments[0];
        var interpolatedValue = (Expression)_evaluated[expressionReference]!;

        return Coerce(resultType, interpolatedValue);
    }

    protected Expression VisitEISpliceBody(MethodCallExpression node) {
        var declaredType = node.Method.GetGenericArguments()[^1];
        var expressionReference = node.Arguments[^1];
        var interpolatedLambda = (LambdaExpression)_evaluated[expressionReference]!;

        // Apply interpolation to the argument replacement expressions
        // N.B. Visit has NotNullIfNotNullAttribute
        var interpolatedArguments = node.Arguments.SkipLast(1).Select(Visit);

        var argumentReplacements = interpolatedLambda.Parameters.Zip(interpolatedArguments).ToDictionary(
            tup => (Expression)tup.First,
            tup => tup.Second!
        );

        return Coerce(declaredType, ExpressionHelpers.Replace(interpolatedLambda.Body, argumentReplacements));
    }

    protected Expression VisitEIValue(MethodCallExpression node) {
        var declaredType = node.Method.GetGenericArguments()[0];
        var expressionReference = node.Arguments[0];
        var interpolatedValue = _evaluated[expressionReference];

        return Coerce(declaredType, Expression.Constant(interpolatedValue));
    }

    protected Expression VisitEIQuote(MethodCallExpression node) {
        var expressionReference = node.Arguments[0];
        var tree = (Expression)_evaluated[expressionReference]!;

        return Expression.Quote(tree);
    }

    private Expression Coerce(Type type, Expression expression) =>
        expression.Type == type ? expression : Expression.Convert(expression, type);
}
