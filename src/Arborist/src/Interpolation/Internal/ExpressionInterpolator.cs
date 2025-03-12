namespace Arborist.Interpolation.Internal;

public sealed class ExpressionInterpolator(ISplicedExpressionEvaluator splicedExpressionEvaluator) {
    /// <summary>
    /// The default <see cref="ExpressionInterpolator"/> instance used by the public
    /// interpolation APIs.
    /// </summary>
    public static ExpressionInterpolator Default { get; } = new(
        splicedExpressionEvaluator: new DefaultSplicedExpressionEvaluator(
            partialExpressionEvaluator: new ReflectivePartialSplicedExpressionEvaluator(
                expressionCompiler: LightExpressionCompiler.Instance
            ),
            expressionEvaluator: new CompilingSplicedExpressionEvaluator(
                expressionCompiler: LightExpressionCompiler.Instance
            )
        )
    );

    public Expression<TDelegate> Interpolate<TData, TDelegate>(TData data, LambdaExpression expression)
        where TDelegate : Delegate
    {
        var analyzer = new AnalyzingInterpolatedExpressionVisitor(expression);
        analyzer.Apply(expression.Body);

        // If there are no expressions requiring evaluation, then there are no splices
        if(analyzer.EvaluatedExpressions.Count == 0)
            return Expression.Lambda<TDelegate>(
                body: expression.Body,
                parameters: expression.Parameters.Skip(1)
            );

        var evaluatedSpliceParameters = splicedExpressionEvaluator.Evaluate<TData>(new(
            data: data,
            dataReferences: analyzer.DataReferences,
            expressions: analyzer.EvaluatedExpressions
        ));

        var splicer = new SplicingInterpolatedExpressionVisitor(evaluatedSpliceParameters);
        var spliced = splicer.Apply(expression.Body);

        return Expression.Lambda<TDelegate>(
            body: spliced,
            parameters: expression.Parameters.Skip(1)
        );
    }
}
