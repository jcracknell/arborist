using Arborist.Internal.Collections;

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
            nameof(InterpolationSpliceOperations.Splice) => VisitSplice(node),
            nameof(InterpolationSpliceOperations.SpliceBody) => VisitSpliceBody(node),
            nameof(InterpolationSpliceOperations.SpliceConstant) => VisitSpliceConstant(node),
            nameof(InterpolationSpliceOperations.SpliceQuoted) => VisitSpliceQuoted(node),
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

        // The leading arguments of the method call supply the replacement expressions for the parameters
        // in the body of the final LambdaExpression, however we don't have access to lambda yet as the
        // expressions are evaluated in order. To avoid allocating a second collection we'll build a
        // replacements array with null target expressions, and then patch it up.
        var argumentReplacementCount = node.Arguments.Count - 2;
        var argumentReplacements = new KeyValuePair<Expression, Expression>[argumentReplacementCount];
        for(var i = 0; i < argumentReplacementCount; i++)
            argumentReplacements[i] = new KeyValuePair<Expression, Expression>(
                default!,
                Visit(node.Arguments[i + 1])
            );

        // Get the lambda now we've processed any splices occurring in the replacement expressions
        var lambdaExpression = GetEvaluatedSpliceParameter<LambdaExpression>();

        // Patch up the replacements with the parameter expressions
        for(var i = 0; i < argumentReplacementCount; i++)
            argumentReplacements[i] = new KeyValuePair<Expression, Expression>(
                lambdaExpression.Parameters[i],
                argumentReplacements[i].Value
            );

        // Type coercion is necessary here to handle the implicit conversion which can occur between
        // a LambdaExpression and its body expression; e.g. an expression producing a System.Object may
        // have a body with type System.String.
        return Coerce(declaredType, ExpressionHelper.Replace(
            lambdaExpression.Body,
            SmallDictionary.Create(argumentReplacements)
        ));
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
