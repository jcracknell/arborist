using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitor {
    // This design is slightly awkward, however we want to track the traversed expression tree node during
    // our recursive visitation of the syntax tree which suggests a disposable-esque approach. Unfortunately
    // because C# usings are not expressions, such an approach would require a method per child node (gack),
    // or the loan pattern, which would cause far too many lambda allocations.
    //
    // As such I have settled on the approach where an initial call binds the descendant expression node
    // in the instance context, which is then consumed by a subsequent visitation occurring in (or before)
    // the call to set the bound value.

    private ExpressionBinding CurrentExpr { get; set; } = default!;
    
    private void SetBoundType(Type expressionType) {
        CurrentExpr.SetType(expressionType);
    }
        
    /// <summary>
    /// Binds the descendant of the current expression tree node identified by the provided <paramref name="binding"/>
    /// as the <see cref="CurrentExpr"/>.
    /// </summary>
    private ExpressionBinding Bind(ref InterpolatedTree.InterpolationHandler binding) =>
        Bind(default!, ref binding);
    
    /// <summary>
    /// Binds the descendant of the current expression tree node identified by the provided <paramref name="binding"/>
    /// as the <see cref="CurrentExpr"/>.
    /// </summary>
    private ExpressionBinding Bind(Type expressionType, ref InterpolatedTree.InterpolationHandler binding) {
        var depth = CurrentExpr?.Depth ?? 0;
    
        return CurrentExpr = new ExpressionBinding(
            parent: CurrentExpr,
            visitor: this,
            identifier: $"__e{depth}",
            binding: BindValue(ref binding),
            expressionType: expressionType
        );
    }
    
    /// <summary>
    /// Creates an <see cref="InterpolatedTree"/> referencing the descendant of the current expression tree node
    /// identified by the provided <paramref name="binding"/>.
    /// </summary>
    private InterpolatedTree BindValue(ref InterpolatedTree.InterpolationHandler binding) =>
        InterpolatedTree.Concat(CurrentExpr.Identifier, InterpolatedTree.Verbatim("."), binding.GetTree());
    
    /// <summary>
    /// Binds the argument of the current expression tree node at the specified <paramref name="index"/>, on the
    /// assumption that the current expression tree node is a <see cref="MethodCallExpression"/> representing
    /// a call to the provided <see cref="IMethodSymbol"/>. In the event that the method is an instance method,
    /// index 0 binds to the <see cref="MethodCallExpression.Object"/> of the method.
    /// </summary>
    private ExpressionBinding BindCallArg(IMethodSymbol methodSymbol, int index) =>
        BindCallArg(default!, methodSymbol, index);
    
    /// <summary>
    /// Binds the argument of the current expression tree node at the specified <paramref name="index"/>, on the
    /// assumption that the current expression tree node is a <see cref="MethodCallExpression"/> representing
    /// a call to the provided <see cref="IMethodSymbol"/>. In the event that the method is an instance method,
    /// index 0 binds to the <see cref="MethodCallExpression.Object"/> of the method.
    /// </summary>
    private ExpressionBinding BindCallArg(Type expressionType, IMethodSymbol methodSymbol, int index) {
        if(methodSymbol is { ReducedFrom: { } } or { IsStatic: true })
            return Bind(expressionType, $"{nameof(MethodCallExpression.Arguments)}[{index}]");
        if(index == 0)
            return Bind(expressionType, $"{nameof(MethodCallExpression.Object)}");
            
        return Bind(expressionType, $"{nameof(MethodCallExpression.Arguments)}[{index - 1}]");
    }

    private sealed class ExpressionBinding {
        public ExpressionBinding(
            ExpressionBinding? parent,
            InterpolatedSyntaxVisitor visitor,
            string identifier,
            InterpolatedTree binding,
            Type? expressionType
        ) {
            _parent = parent;
            _visitor = visitor;
            _identifierString = identifier;
            Identifier = InterpolatedTree.Verbatim(identifier);
            _binding = binding;
            _expressionType = expressionType;
        }
    
        private readonly ExpressionBinding? _parent;
        private readonly InterpolatedSyntaxVisitor _visitor;
        private readonly string _identifierString;
        public InterpolatedTree Identifier { get; }
        private readonly InterpolatedTree _binding;
        private Type? _expressionType;
        
        public int Depth => (_parent?.Depth ?? 0) + 1;
        
        public void SetType(Type type) {
            if(_expressionType is not null && !_expressionType.IsAssignableFrom(type))
                throw new InvalidOperationException($"Invalid attempt to rebind expression node type from {_expressionType} to {type}.");
                
            _expressionType = type;
        }
        
        public InterpolatedTree WithValue(InterpolatedTree value) {
            if(_expressionType is null && value.IsSupported)
                throw new InvalidOperationException($"Expression type is not set for body: {value}");
            if(!ReferenceEquals(this, _visitor.CurrentExpr))
                throw new InvalidOperationException($"{nameof(Bind)} calls must be immediately followed by a call to {nameof(WithValue)}.");
                
            // Restore the parent expression node as the current node.
            _visitor.CurrentExpr = _parent!;
                
            // If the provided value is unmodified, we can return the input node binding directly instead of the
            // generated replacement tree. This is how the interpolation process retains subtrees which are unmodified
            // and do not contain splices.
            if(!value.IsModified)
                return _binding;
                
            var typedBinding = _expressionType is null ? _binding : InterpolatedTree.Concat([
                InterpolatedTree.Verbatim($"(global::{_expressionType.FullName})("),
                _binding,
                InterpolatedTree.Verbatim(")")
            ]);
            
            return InterpolatedTree.Bind(_identifierString, typedBinding, value);
        }
    }
}
