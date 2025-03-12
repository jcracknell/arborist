namespace Arborist.Interpolation.Internal;

public readonly struct SplicedExpressionEvaluationContext<TData>(
    TData data,
    IReadOnlySet<Expression> dataReferences,
    IReadOnlyList<Expression> expressions
) {
    /// <summary>
    /// The data provided to the interpolation process.
    /// </summary>
    public TData Data { get; } = data;

    /// <summary>
    /// The subtrees occurring in the provided expressions referencing the data provided to the
    /// interpolation process.
    /// </summary>
    public IReadOnlySet<Expression> DataReferences { get; } = dataReferences;

    /// <summary>
    /// The list of expressions requiring evaluation.
    /// </summary>
    public IReadOnlyList<Expression> Expressions { get; } = expressions;
}
