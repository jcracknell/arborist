using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

// This design is slightly awkward, however we want to track the traversed expression tree node during
// our recursive visitation of the syntax tree which suggests a disposable-esque approach. Unfortunately
// because C# usings are not expressions, such an approach would require a method per child node (gack),
// or the loan pattern, which would cause far too many lambda allocations.
//
// As such I have settled on the approach where an initial call binds the descendant expression node
// in the instance context, which is then consumed by a subsequent visitation occurring in (or before)
// the call to set the bound value.

public abstract class ExpressionBinding(
    ExpressionBinding? parent,
    InterpolatedTree binding,
    Type? expressionType
) {
    protected ExpressionBinding? Parent { get; } = parent;
    protected InterpolatedTree Binding { get; } = binding;
    protected Type? ExpressionType { get; private set; } = expressionType;

    protected abstract ExpressionBinding GetCurrent();
    protected abstract void SetCurrent(ExpressionBinding? value);
    protected abstract InterpolatedTree CreateResult(InterpolatedTree value);

    public virtual void SetType(Type type) {
        if(ExpressionType is not null && !ExpressionType.IsAssignableFrom(type))
            throw new InvalidOperationException($"Invalid attempt to rebind expression node type from {ExpressionType} to {type}.");

        ExpressionType = type;
    }

    public InterpolatedTree WithValue(InterpolatedTree value) {
        if(!ReferenceEquals(this, GetCurrent()))
            throw new InvalidOperationException($"Subject {nameof(ExpressionBinding)} is not the current expression.");

        // Restore the parent expression node as the current node.
        SetCurrent(Parent);
        return CreateResult(value);
    }

    protected abstract ExpressionBinding BindDescendant(Type? expressionType, ref InterpolatedTree.InterpolationHandler binding);

    /// <summary>
    /// Binds the descendant of the current expression tree node identified by the provided <paramref name="binding"/>
    /// as the current expression.
    /// </summary>
    public ExpressionBinding Bind(ref InterpolatedTree.InterpolationHandler binding) =>
        Bind(default!, ref binding);

    /// <summary>
    /// Binds the descendant of the current expression tree node identified by the provided <paramref name="binding"/>
    /// as the current expression.
    /// </summary>
    public ExpressionBinding Bind(Type expressionType, ref InterpolatedTree.InterpolationHandler binding) {
        var bound = BindDescendant(expressionType, ref binding);
        SetCurrent(bound);
        return bound;
    }

    /// <summary>
    /// Binds the argument of the current expression tree node at the specified <paramref name="index"/>, on the
    /// assumption that the current expression tree node is a <see cref="MethodCallExpression"/> representing
    /// a call to the provided <see cref="IMethodSymbol"/>. In the event that the method is an instance method,
    /// index 0 binds to the <see cref="MethodCallExpression.Object"/> of the method.
    /// </summary>
    public ExpressionBinding BindCallArg(IMethodSymbol methodSymbol, int index) =>
        BindCallArg(default!, methodSymbol, index);

    /// <summary>
    /// Binds the argument of the current expression tree node at the specified <paramref name="index"/>, on the
    /// assumption that the current expression tree node is a <see cref="MethodCallExpression"/> representing
    /// a call to the provided <see cref="IMethodSymbol"/>. In the event that the method is an instance method,
    /// index 0 binds to the <see cref="MethodCallExpression.Object"/> of the method.
    /// </summary>
    public ExpressionBinding BindCallArg(Type expressionType, IMethodSymbol methodSymbol, int index) {
        if(methodSymbol is { ReducedFrom: { } } or { IsStatic: true })
            return Bind(expressionType, $"{nameof(MethodCallExpression.Arguments)}[{index}]");
        if(index == 0)
            return Bind(expressionType, $"{nameof(MethodCallExpression.Object)}");

        return Bind(expressionType, $"{nameof(MethodCallExpression.Arguments)}[{index - 1}]");
    }
}
