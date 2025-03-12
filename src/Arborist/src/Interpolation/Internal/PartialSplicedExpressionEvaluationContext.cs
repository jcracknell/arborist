namespace Arborist.Interpolation.Internal;

public readonly struct PartialSplicedExpressionEvaluationContext<TData>(
    TData data,
    IReadOnlySet<Expression> dataReferences,
    Expression expression
) {
    public TData Data { get; } = data;
    public IReadOnlySet<Expression> DataReferences { get; } = dataReferences;
    public Expression Expression { get; } = expression;
}
