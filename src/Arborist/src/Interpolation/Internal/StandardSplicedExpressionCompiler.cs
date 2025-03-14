namespace Arborist.Interpolation.Internal;

/// <summary>
/// <see cref="ISplicedExpressionCompiler"/> implementation using the standard compilation
/// mechanism, <see cref="LambdaExpression.Compile()"/>.
/// </summary>
public sealed class StandardSplicedExpressionCompiler : ISplicedExpressionCompiler {
    public static StandardSplicedExpressionCompiler Instance { get; } = new();

    private StandardSplicedExpressionCompiler() { }

    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate =>
        expression.Compile();

    public Delegate Compile(LambdaExpression expression) =>
        expression.Compile();
}
