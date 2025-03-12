namespace Arborist.Interpolation.Internal;

public interface IExpressionCompiler {
    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;

    public Delegate Compile(LambdaExpression expression);
}
