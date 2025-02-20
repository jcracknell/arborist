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

public abstract class ExpressionBinding {
    public static string CreateIdentifier(int depth) =>
        $"__e{depth}";

    private readonly ExpressionBinding? _parent;
    private readonly string _identifierString;
    private InterpolatedTree? _identifier;
    private readonly InterpolatedTree _binding;
    private Type? _expressionType;

    public ExpressionBinding(
        ExpressionBinding? parent,
        string identifierString,
        InterpolatedTree binding,
        Type? expressionType
    ) {
        _parent = parent;
        _identifierString = identifierString;
        _binding = binding;
        _expressionType = expressionType;
    }

    protected abstract ExpressionBinding GetCurrent();
    protected abstract void SetCurrent(ExpressionBinding? value);
    protected abstract InterpolatedTree GetUnmarkedValue(InterpolatedTree binding, InterpolatedTree value);
    protected abstract ExpressionBinding Bind(string identifier, InterpolatedTree binding, Type? expressionType);

    public InterpolatedTree Identifier =>
        _identifier ??= InterpolatedTree.Verbatim(_identifierString);

    public int Depth => (_parent?.Depth + 1) ?? 0;

    public bool IsMarked { get; private set; }

    /// <summary>
    /// Marks the subject <see cref="ExpressionBinding"/>, signaling that the bound expression has been
    /// altered in some way in the result tree.
    /// </summary>
    public void Mark() {
        IsMarked = true;
        _parent?.Mark();
    }

    public void SetType(Type type) {
        if(_expressionType is not null && !_expressionType.IsAssignableFrom(type))
            throw new InvalidOperationException($"Invalid attempt to rebind expression node type from {_expressionType} to {type}.");

        _expressionType = type;
    }

    public InterpolatedTree WithValue(InterpolatedTree value) {
        if(_expressionType is null && value.IsSupported)
            throw new InvalidOperationException($"Expression type is not set for body: {value}");
        if(!ReferenceEquals(this, GetCurrent()))
            throw new InvalidOperationException($"Subject {nameof(ExpressionBinding)} is not the current expression.");

        // Restore the parent expression node as the current node.
        SetCurrent(_parent);

        // If the provided value is unmarked, we defer to the implementation as to which tree should be returned
        if(!IsMarked && value.IsSupported)
            return GetUnmarkedValue(_binding, value);

        var typedBinding = _expressionType is null ? _binding : InterpolatedTree.Concat([
            InterpolatedTree.Verbatim($"(global::{_expressionType.FullName})("),
            _binding,
            InterpolatedTree.Verbatim(")")
        ]);

        return InterpolatedTree.Bind(_identifierString, typedBinding, value);
    }

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
        var depth = Depth + 1;
        var bound = Bind(
            identifier: CreateIdentifier(depth),
            binding: BindValue(ref binding),
            expressionType: expressionType
        );

        SetCurrent(bound);
        return bound;
    }

    /// <summary>
    /// Creates an <see cref="InterpolatedTree"/> referencing the descendant of the current expression tree node
    /// identified by the provided <paramref name="binding"/>.
    /// </summary>
    public InterpolatedTree BindValue(ref InterpolatedTree.InterpolationHandler binding) =>
        InterpolatedTree.Concat(GetCurrent().Identifier, InterpolatedTree.Verbatim("."), binding.GetTree());

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
