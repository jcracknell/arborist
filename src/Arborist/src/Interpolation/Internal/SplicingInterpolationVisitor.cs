namespace Arborist.Interpolation.Internal;

public class SplicingInterpolationVisitor : InterpolationVisitor {
    private readonly IReadOnlyList<object?> _evaluatedSpliceParameters;
    private int _evaluatedSpliceParameterIndex;

    public SplicingInterpolationVisitor(IReadOnlyList<object?> evaluatedSpliceParameters) {
        _evaluatedSpliceParameters = evaluatedSpliceParameters;
        _evaluatedSpliceParameterIndex = 0;
    }

    public Expression Apply(Expression expression) {
        var result = Visit(expression);

        if(_evaluatedSpliceParameterIndex != _evaluatedSpliceParameters.Count)
            throw new Exception("Failed to consume all evaluated splice parameters?");

        return result;
    }

    private T GetEvaluatedSpliceParameter<T>(bool increment = true) =>
        GetEvaluatedSpliceParameter<T>(increment ? _evaluatedSpliceParameterIndex++ : _evaluatedSpliceParameterIndex);

    private T GetEvaluatedSpliceParameter<T>(int index) =>
        (T)_evaluatedSpliceParameters[index]!;

    protected override Expression VisitSplicingMethodCall(MethodCallExpression node) {
        return node.Method.Name switch {
            nameof(IInterpolationContext.Splice) => VisitSplice(node),
            nameof(IInterpolationContext.SpliceBody) => VisitSpliceBody(node),
            nameof(IInterpolationContext.SpliceConstant) => VisitSpliceConstant(node),
            nameof(IInterpolationContext.SpliceQuoted) => VisitSpliceQuoted(node),
            _ => throw new Exception($"Unhandled {typeof(IInterpolationContext)} method: {node.Method}.")
        };
    }

    private Expression VisitSplice(MethodCallExpression node) {
        var resultType = node.Method.GetGenericArguments()[0];
        var interpolatedValue = GetEvaluatedSpliceParameter<Expression>();

        return Coerce(resultType, interpolatedValue);
    }

    private Expression VisitSpliceBody(MethodCallExpression node) {
        var declaredType = node.Method.GetGenericArguments()[^1];
        var interpolatedLambda = GetEvaluatedSpliceParameter<LambdaExpression>();

        // Apply interpolation to the argument replacement expressions
        // N.B. Visit has NotNullIfNotNullAttribute
        var interpolatedArguments = node.Arguments.SkipLast(1).Select(Visit);

        var argumentReplacements = interpolatedLambda.Parameters.Zip(interpolatedArguments).ToDictionary(
            tup => (Expression)tup.First,
            tup => tup.Second!
        );

        // Type coercion is necessary here to handle the implicit conversion which can occur between
        // a LambdaExpression and its body expression; e.g. an expression producing a System.Object may
        // have a body with type System.String.
        return Coerce(declaredType, ExpressionHelper.Replace(interpolatedLambda.Body, argumentReplacements));
    }

    private Expression VisitSpliceConstant(MethodCallExpression node) {
        var declaredType = node.Method.GetGenericArguments()[0];
        var interpolatedValue = GetEvaluatedSpliceParameter<object?>();

        // No coercion of the expression type is necessary, as any required conversion is reflected
        // in the evaluated argument expression
        return Expression.Constant(interpolatedValue, declaredType);
    }

    private Expression VisitSpliceQuoted(MethodCallExpression node) {
        var tree = GetEvaluatedSpliceParameter<Expression>();

        return Expression.Quote(tree);
    }

    private Expression Coerce(Type type, Expression expression) =>
        expression.Type == type ? expression : Expression.Convert(expression, type);
}
