namespace Arborist.Interpolation.Internal;

/// <summary>
/// <see cref="ISplicedExpressionCompiler"/> implementation using the internal expression interpreter
/// shipped in System.Linq.Expressions which is used as a fallback in the event that IL emission
/// is not possible. This is substantially faster (in terms of compilation speed) than the
/// default IL-based compiler.
/// </summary>
public sealed class LightSplicedExpressionCompiler : ISplicedExpressionCompiler {
    public static LightSplicedExpressionCompiler Instance { get; } = new();

    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate =>
        (TDelegate)Compile((LambdaExpression)expression);

    public Delegate Compile(LambdaExpression expression) =>
        LightCompile?.Invoke(expression) ?? expression.Compile();

    private static readonly Func<LambdaExpression, Delegate>? LightCompile = CreateLightCompiler();

    private static Func<LambdaExpression, Delegate>? CreateLightCompiler() {
        var assembly = typeof(Expression).Assembly;

        var lightCompiler = assembly.GetType("System.Linq.Expressions.Interpreter.LightCompiler");
        if(lightCompiler is null)
            return null;

        try {
            var lambdaParameter = Expression.Parameter(typeof(LambdaExpression));

            return Expression.Lambda<Func<LambdaExpression, Delegate>>(
                Expression.Call(
                    Expression.Call(
                        Expression.New(lightCompiler.GetConstructor(Type.EmptyTypes)!),
                        lightCompiler.GetMethod("CompileTop", new[] { typeof(LambdaExpression) })!,
                        lambdaParameter
                    ),
                    assembly.GetType("System.Linq.Expressions.Interpreter.LightDelegateCreator")!
                    .GetMethod("CreateDelegate", Type.EmptyTypes)!
                ),
                lambdaParameter
            )
            .Compile();
        } catch {
            return null;
        }
    }
}
