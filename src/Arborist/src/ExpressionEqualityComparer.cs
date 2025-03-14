using System.Reflection;

namespace Arborist;

/// <summary>
/// Structural <see cref="IEqualityComparer{T}"/> implementation for <see cref="Expression"/>
/// instances.
/// </summary>
public sealed class ExpressionEqualityComparer : IEqualityComparer<Expression?> {
    // EFCore has a similar implementation:
    // https://github.com/dotnet/efcore/blob/main/src/EFCore/Query/ExpressionEqualityComparer.cs
    // however it has "odd" rules around parameter and constant equality, whereas we only care about
    // pure structural equality. This allows us to optimize the sequence hashing/equality implementation
    // based on co-recursive IEqualityComparer implementations.

    /// <summary>
    /// The default <see cref="ExpressionEqualityComparer"/> instance using the default
    /// <see cref="IEqualityComparer{T}"/> implementation for constant value comparisons.
    /// </summary>
    public static ExpressionEqualityComparer Default { get; } = new(EqualityComparer<object?>.Default);

    private readonly IEqualityComparer<object?> _constantComparer;
    private readonly CatchBlockEqualityComparer _catchBlockComparer;
    private readonly ElementInitEqualityComparer _elementInitComparer;
    private readonly MemberBindingEqualityComparer _memberBindingComparer;
    private readonly SwitchCaseEqualityComparer _switchCaseComparer;

    /// <summary>
    /// Creates an <see cref="ExpressionEqualityComparer"/> which uses the provided
    /// <paramref name="constantComparer"/> to compare values captured by <see cref="ConstantExpression"/>
    /// instances within expression trees.
    /// </summary>
    /// <param name="constantComparer">
    /// <see cref="IEqualityComparer{T}"/> used to compare <see cref="ConstantExpression"/> values
    /// occurring within expression trees.
    /// </param>
    public ExpressionEqualityComparer(IEqualityComparer<object?> constantComparer) {
        _constantComparer = constantComparer;
        _catchBlockComparer = new(expressionComparer: this);
        _elementInitComparer = new(expressionComparer: this);
        _memberBindingComparer = new(expressionComparer: this, elementInitComparer: _elementInitComparer);
        _switchCaseComparer = new(expressionComparer: this);
    }

    private static Exception UnsupportedExpressionException(Expression expression) =>
        new NotImplementedException($"Unsupported {nameof(ExpressionType)}: {expression.NodeType}.");

    private static Exception UnsupportedMemberBindingException(MemberBinding memberBinding) =>
        new NotImplementedException($"Unsupported {nameof(MemberBindingType)}: {memberBinding.BindingType}.");

    public int GetHashCode(Expression expression) {
        if(expression is null)
            return 0;

        var hash = new HashCode();
        hash.Add(expression.NodeType);
        hash.Add(expression.Type);

        return expression switch {
            BinaryExpression => HashBinary(ref hash, (BinaryExpression)expression),
            BlockExpression => HashBlock(ref hash, (BlockExpression)expression),
            ConditionalExpression => HashConditional(ref hash, (ConditionalExpression)expression),
            ConstantExpression => HashConstant(ref hash, (ConstantExpression)expression),
            DebugInfoExpression => HashDebugInfo(ref hash, (DebugInfoExpression)expression),
            DefaultExpression => hash.ToHashCode(),
            DynamicExpression => HashDynamic(ref hash, (DynamicExpression)expression),
            GotoExpression => HashGoto(ref hash, (GotoExpression)expression),
            IndexExpression => HashIndex(ref hash, (IndexExpression)expression),
            InvocationExpression => HashInvocation(ref hash, (InvocationExpression)expression),
            LabelExpression => HashLabel(ref hash, (LabelExpression)expression),
            LambdaExpression => HashLambda(ref hash, (LambdaExpression)expression),
            ListInitExpression => HashListInit(ref hash, (ListInitExpression)expression),
            LoopExpression => HashLoop(ref hash, (LoopExpression)expression),
            MemberExpression => HashMember(ref hash, (MemberExpression)expression),
            MemberInitExpression => HashMemberInit(ref hash, (MemberInitExpression)expression),
            MethodCallExpression => HashMethodCall(ref hash, (MethodCallExpression)expression),
            NewArrayExpression => HashNewArray(ref hash, (NewArrayExpression)expression),
            NewExpression => HashNew(ref hash, (NewExpression)expression),
            ParameterExpression => HashParameter(ref hash, (ParameterExpression)expression),
            RuntimeVariablesExpression => HashRuntimeVariables(ref hash, (RuntimeVariablesExpression)expression),
            SwitchExpression => HashSwitch(ref hash, (SwitchExpression)expression),
            TryExpression => HashTry(ref hash, (TryExpression)expression),
            TypeBinaryExpression => HashTypeBinary(ref hash, (TypeBinaryExpression)expression),
            UnaryExpression => HashUnary(ref hash, (UnaryExpression)expression),
            _ => throw UnsupportedExpressionException(expression)
        };
    }

    public bool Equals(Expression? a, Expression? b) {
        if(ReferenceEquals(a, b))
            return true;
        if(a is null || b is null)
            return false;
        if(a.NodeType != b.NodeType)
            return false;
        if(a.Type != b.Type)
            return false;

        return a switch {
            BinaryExpression => EquateBinary((BinaryExpression)a, (BinaryExpression)b),
            BlockExpression => EquateBlock((BlockExpression)a, (BlockExpression)b),
            ConditionalExpression => EquateConditional((ConditionalExpression)a, (ConditionalExpression)b),
            ConstantExpression => EquateConstant((ConstantExpression)a, (ConstantExpression)b),
            DebugInfoExpression => EquateDebugInfo((DebugInfoExpression)a, (DebugInfoExpression)b),
            DefaultExpression => true,
            DynamicExpression => EquateDynamic((DynamicExpression)a, (DynamicExpression)b),
            GotoExpression => EquateGoto((GotoExpression)a, (GotoExpression)b),
            IndexExpression => EquateIndex((IndexExpression)a, (IndexExpression)b),
            InvocationExpression => EquateInvocation((InvocationExpression)a, (InvocationExpression)b),
            LabelExpression => EquateLabel((LabelExpression)a, (LabelExpression)b),
            LambdaExpression => EquateLambda((LambdaExpression)a, (LambdaExpression)b),
            ListInitExpression => EquateListInit((ListInitExpression)a, (ListInitExpression)b),
            LoopExpression => EquateLoop((LoopExpression)a, (LoopExpression)b),
            MemberExpression => EquateMember((MemberExpression)a, (MemberExpression)b),
            MemberInitExpression => EquateMemberInit((MemberInitExpression)a, (MemberInitExpression)b),
            MethodCallExpression => EquateMethodCall((MethodCallExpression)a, (MethodCallExpression)b),
            NewArrayExpression => EquateNewArray((NewArrayExpression)a, (NewArrayExpression)b),
            NewExpression => EquateNew((NewExpression)a, (NewExpression)b),
            ParameterExpression => EquateParameter((ParameterExpression)a, (ParameterExpression)b),
            RuntimeVariablesExpression => EquateRuntimeVariables((RuntimeVariablesExpression)a, (RuntimeVariablesExpression)b),
            SwitchExpression => EquateSwitch((SwitchExpression)a, (SwitchExpression)b),
            TryExpression => EquateTry((TryExpression)a, (TryExpression)b),
            TypeBinaryExpression => EquateTypeBinary((TypeBinaryExpression)a, (TypeBinaryExpression)b),
            UnaryExpression => EquateUnary((UnaryExpression)a, (UnaryExpression)b),
            _ => throw UnsupportedExpressionException(a)
        };
    }

    private int HashBinary(ref HashCode hash, BinaryExpression binaryExpression) {
        hash.Add(binaryExpression.IsLifted);
        hash.Add(binaryExpression.IsLiftedToNull);
        hash.Add(binaryExpression.Method);
        hash.Add(binaryExpression.Conversion, this);
        hash.Add(binaryExpression.Left, this);
        hash.Add(binaryExpression.Right, this);
        return hash.ToHashCode();
    }

    private bool EquateBinary(BinaryExpression a, BinaryExpression b) =>
        a.IsLifted == b.IsLifted
        && a.IsLiftedToNull == b.IsLiftedToNull
        && Equals(a.Method, b.Method)
        && Equals(a.Left, b.Left)
        && Equals(a.Right, b.Right)
        && Equals(a.Conversion, b.Conversion);

    private int HashBlock(ref HashCode hash, BlockExpression blockExpression) {
        SequenceHash(ref hash, blockExpression.Variables, this);
        SequenceHash(ref hash, blockExpression.Expressions, this);
        hash.Add(blockExpression.Result, this);
        return hash.ToHashCode();
    }

    private bool EquateBlock(BlockExpression a, BlockExpression b) =>
        Equals(a.Result, b.Result)
        && SequenceEqual(a.Variables, b.Variables, this)
        && SequenceEqual(a.Expressions, b.Expressions, this);

    private int HashConditional(ref HashCode hash, ConditionalExpression conditionalExpression) {
        hash.Add(conditionalExpression.Test, this);
        hash.Add(conditionalExpression.IfTrue, this);
        hash.Add(conditionalExpression.IfFalse, this);
        return hash.ToHashCode();
    }

    private bool EquateConditional(ConditionalExpression a, ConditionalExpression b) =>
        Equals(a.Test, b.Test)
        && Equals(a.IfTrue, b.IfTrue)
        && Equals(a.IfFalse, b.IfFalse);

    private int HashConstant(ref HashCode hash, ConstantExpression constantExpression) {
        hash.Add(constantExpression.Value, _constantComparer);
        return hash.ToHashCode();
    }

    private bool EquateConstant(ConstantExpression a, ConstantExpression b) =>
        _constantComparer.Equals(a.Value, b.Value);

    private int HashDebugInfo(ref HashCode hash, DebugInfoExpression debugInfoExpression) {
        hash.Add(debugInfoExpression.IsClear);
        hash.Add(debugInfoExpression.StartLine);
        hash.Add(debugInfoExpression.StartColumn);
        hash.Add(debugInfoExpression.EndLine);
        hash.Add(debugInfoExpression.EndColumn);
        hash.Add(debugInfoExpression.Document);
        return hash.ToHashCode();
    }

    private bool EquateDebugInfo(DebugInfoExpression a, DebugInfoExpression b) =>
        a.IsClear == b.IsClear
        && a.StartLine == b.StartLine
        && a.StartColumn == b.StartColumn
        && a.EndLine == b.EndLine
        && a.EndColumn == b.EndColumn
        && Equals(a.Document, b.Document);

    private int HashDynamic(ref HashCode hash, DynamicExpression dynamicExpression) {
        hash.Add(dynamicExpression.Binder);
        hash.Add(dynamicExpression.DelegateType);
        SequenceHash(ref hash, dynamicExpression.Arguments, this);
        return hash.ToHashCode();
    }

    private bool EquateDynamic(DynamicExpression a, DynamicExpression b) =>
        Equals(a.Binder, b.Binder)
        && Equals(a.DelegateType, b.DelegateType)
        && SequenceEqual(a.Arguments, b.Arguments, this);

    private int HashGoto(ref HashCode hash, GotoExpression gotoExpression) {
        hash.Add(gotoExpression.Kind);
        hash.Add(gotoExpression.Target);
        hash.Add(gotoExpression.Value, this);
        return hash.ToHashCode();
    }

    private bool EquateGoto(GotoExpression a, GotoExpression b) =>
        a.Kind == b.Kind
        && Equals(a.Target, b.Target)
        && Equals(a.Value, b.Value);

    private int HashIndex(ref HashCode hash, IndexExpression indexExpression) {
        hash.Add(indexExpression.Object, this);
        hash.Add(indexExpression.Indexer);
        SequenceHash(ref hash, indexExpression.Arguments, this);
        return hash.ToHashCode();
    }

    private bool EquateIndex(IndexExpression a, IndexExpression b) =>
        Equals(a.Object, b.Object)
        && Equals(a.Indexer, b.Indexer)
        && SequenceEqual(a.Arguments, b.Arguments, this);

    private int HashInvocation(ref HashCode hash, InvocationExpression invocationExpression) {
        hash.Add(invocationExpression.Expression, this);
        SequenceHash(ref hash, invocationExpression.Arguments, this);
        return hash.ToHashCode();
    }

    private bool EquateInvocation(InvocationExpression a, InvocationExpression b) =>
        Equals(a.Expression, b.Expression)
        && SequenceEqual(a.Arguments, b.Arguments, this);

    private int HashLabel(ref HashCode hash, LabelExpression labelExpression) {
        hash.Add(labelExpression.DefaultValue, this);
        hash.Add(labelExpression.Target);
        return hash.ToHashCode();
    }

    private bool EquateLabel(LabelExpression a, LabelExpression b) =>
        Equals(a.Target, b.Target)
        && Equals(a.DefaultValue, b.DefaultValue);

    private int HashLambda(ref HashCode hash, LambdaExpression lambdaExpression) {
        SequenceHash(ref hash, lambdaExpression.Parameters, this);
        hash.Add(lambdaExpression.Body, this);
        hash.Add(lambdaExpression.ReturnType);
        return hash.ToHashCode();
    }

    private bool EquateLambda(LambdaExpression a, LambdaExpression b) =>
        Equals(a.ReturnType, b.ReturnType)
        && Equals(a.Body, b.Body)
        && SequenceEqual(a.Parameters, b.Parameters, this);

    private int HashListInit(ref HashCode hash, ListInitExpression listInitExpression) {
        hash.Add(listInitExpression.NewExpression, this);
        SequenceHash(ref hash, listInitExpression.Initializers, _elementInitComparer);
        return hash.ToHashCode();
    }

    private bool EquateListInit(ListInitExpression a, ListInitExpression b) =>
        Equals(a.NewExpression, b.NewExpression)
        && SequenceEqual(a.Initializers, b.Initializers, _elementInitComparer);

    private int HashLoop(ref HashCode hash, LoopExpression loopExpression) {
        hash.Add(loopExpression.Body, this);
        hash.Add(loopExpression.BreakLabel);
        hash.Add(loopExpression.ContinueLabel);
        return hash.ToHashCode();
    }

    private bool EquateLoop(LoopExpression a, LoopExpression b) =>
        Equals(a.Body, b.Body)
        && Equals(a.BreakLabel, b.BreakLabel)
        && Equals(a.ContinueLabel, b.ContinueLabel);

    private int HashMember(ref HashCode hash, MemberExpression memberExpression) {
        hash.Add(memberExpression.Expression, this);
        hash.Add(memberExpression.Member);
        return hash.ToHashCode();
    }

    private bool EquateMember(MemberExpression a, MemberExpression b) =>
        Equals(a.Member, b.Member)
        && Equals(a.Expression, b.Expression);

    private int HashMemberInit(ref HashCode hash, MemberInitExpression memberInitExpression) {
        hash.Add(memberInitExpression.NewExpression, this);
        SequenceHash(ref hash, memberInitExpression.Bindings, _memberBindingComparer);
        return hash.ToHashCode();
    }

    private bool EquateMemberInit(MemberInitExpression a, MemberInitExpression b) =>
        Equals(a.NewExpression, b.NewExpression)
        && SequenceEqual(a.Bindings, b.Bindings, _memberBindingComparer);

    private int HashMethodCall(ref HashCode hash, MethodCallExpression methodCallExpression) {
        hash.Add(methodCallExpression.Object, this);
        hash.Add(methodCallExpression.Method);
        SequenceHash(ref hash, methodCallExpression.Arguments, this);
        return hash.ToHashCode();
    }

    private bool EquateMethodCall(MethodCallExpression a, MethodCallExpression b) =>
        Equals(a.Method, b.Method)
        && Equals(a.Object, b.Object)
        && SequenceEqual(a.Arguments, b.Arguments, this);

    private int HashNewArray(ref HashCode hash, NewArrayExpression newArrayExpression) {
        SequenceHash(ref hash, newArrayExpression.Expressions, this);
        return hash.ToHashCode();
    }

    private bool EquateNewArray(NewArrayExpression a, NewArrayExpression b) =>
        SequenceEqual(a.Expressions, b.Expressions, this);

    private int HashNew(ref HashCode hash, NewExpression newExpression) {
        hash.Add(newExpression.Constructor);
        SequenceHash(ref hash, newExpression.Arguments, this);
        SequenceHash(ref hash, newExpression.Members, EqualityComparer<MemberInfo>.Default);
        return hash.ToHashCode();
    }

    private bool EquateNew(NewExpression a, NewExpression b) =>
        Equals(a.Constructor, b.Constructor)
        && SequenceEqual(a.Arguments, b.Arguments, this)
        && SequenceEqual(a.Members, b.Members, EqualityComparer<MemberInfo>.Default);

    private int HashParameter(ref HashCode hash, ParameterExpression parameterExpression) {
        hash.Add(parameterExpression.Name);
        hash.Add(parameterExpression.IsByRef);
        return hash.ToHashCode();
    }

    private bool EquateParameter(ParameterExpression a, ParameterExpression b) =>
        a.IsByRef == b.IsByRef
        && Equals(a.Name, b.Name);

    private int HashRuntimeVariables(ref HashCode hash, RuntimeVariablesExpression runtimeVariablesExpression) {
        SequenceHash(ref hash, runtimeVariablesExpression.Variables, this);
        return hash.ToHashCode();
    }

    private bool EquateRuntimeVariables(RuntimeVariablesExpression a, RuntimeVariablesExpression b) =>
        SequenceEqual(a.Variables, b.Variables, this);

    private int HashSwitch(ref HashCode hash, SwitchExpression switchExpression) {
        hash.Add(switchExpression.SwitchValue, this);
        hash.Add(switchExpression.Comparison);
        SequenceHash(ref hash, switchExpression.Cases, _switchCaseComparer);
        hash.Add(switchExpression.DefaultBody, this);
        return hash.ToHashCode();
    }

    private bool EquateSwitch(SwitchExpression a, SwitchExpression b) =>
        Equals(a.SwitchValue, b.SwitchValue)
        && Equals(a.Comparison, b.Comparison)
        && Equals(a.DefaultBody, b.DefaultBody)
        && SequenceEqual(a.Cases, b.Cases, _switchCaseComparer);

    private int HashTry(ref HashCode hash, TryExpression tryExpression) {
        hash.Add(tryExpression.Body, this);
        SequenceHash(ref hash, tryExpression.Handlers, _catchBlockComparer);
        hash.Add(tryExpression.Fault, this);
        hash.Add(tryExpression.Finally, this);
        return hash.ToHashCode();
    }

    private bool EquateTry(TryExpression a, TryExpression b) =>
        Equals(a.Body, b.Body)
        && SequenceEqual(a.Handlers, b.Handlers, _catchBlockComparer)
        && Equals(a.Fault, b.Fault)
        && Equals(a.Finally, b.Finally);

    private int HashTypeBinary(ref HashCode hash, TypeBinaryExpression typeBinaryExpression) {
        hash.Add(typeBinaryExpression.Expression, this);
        hash.Add(typeBinaryExpression.TypeOperand);
        return hash.ToHashCode();
    }

    private bool EquateTypeBinary(TypeBinaryExpression a, TypeBinaryExpression b) =>
        Equals(a.Expression, b.Expression)
        && Equals(a.TypeOperand, b.TypeOperand);

    private int HashUnary(ref HashCode hash, UnaryExpression unaryExpression) {
        hash.Add(unaryExpression.Operand, this);
        hash.Add(unaryExpression.Method);
        hash.Add(unaryExpression.IsLifted);
        hash.Add(unaryExpression.IsLiftedToNull);
        return hash.ToHashCode();
    }

    private bool EquateUnary(UnaryExpression a, UnaryExpression b) =>
        a.IsLifted == b.IsLifted
        && a.IsLiftedToNull == b.IsLiftedToNull
        && Equals(a.Method, b.Method)
        && Equals(a.Operand, b.Operand);

    private static void SequenceHash<A>(
        ref HashCode hash,
        IReadOnlyList<A>? list,
        IEqualityComparer<A> equalityComparer
    ) {
        if(list is null)
            return;

        var count = list.Count;
        hash.Add(count);

        for(var i = 0; i < count; i++)
            hash.Add(list[i], equalityComparer);
    }

    private static bool SequenceEqual<A>(
        IReadOnlyList<A>? a,
        IReadOnlyList<A>? b,
        IEqualityComparer<A> equalityComparer
    ) {
        if(ReferenceEquals(a, b))
            return true;
        if(a is null || b is null)
            return false;

        var count = a.Count;
        if(b.Count != count)
            return false;

        for(var i = 0; i < count; i++)
            if(!equalityComparer.Equals(a[i], b[i]))
                return false;

        return true;
    }

    private sealed class CatchBlockEqualityComparer(IEqualityComparer<Expression?> expressionComparer)
        : IEqualityComparer<CatchBlock?>
    {
        public int GetHashCode(CatchBlock obj) {
            if(obj is null)
                return 0;

            var hash = new HashCode();
            hash.Add(obj.Test);
            hash.Add(obj.Variable, expressionComparer);
            hash.Add(obj.Filter, expressionComparer);
            hash.Add(obj.Body, expressionComparer);
            return hash.ToHashCode();
        }

        public bool Equals(CatchBlock? x, CatchBlock? y) {
            if(ReferenceEquals(x, y))
                return true;
            if(x is null || y is null)
                return false;

            return Equals(x.Test, y.Test)
                && expressionComparer.Equals(x.Variable, y.Variable)
                && expressionComparer.Equals(x.Filter, y.Filter)
                && expressionComparer.Equals(x.Body, y.Body);
        }
    }

    private sealed class ElementInitEqualityComparer(IEqualityComparer<Expression?> expressionComparer)
        : IEqualityComparer<ElementInit?>
    {
        public int GetHashCode(ElementInit obj) {
            if(obj is null)
                return 0;

            var hash = new HashCode();
            hash.Add(obj.AddMethod);
            SequenceHash(ref hash, obj.Arguments, expressionComparer);
            return hash.ToHashCode();
        }

        public bool Equals(ElementInit? x, ElementInit? y) {
            if(ReferenceEquals(x, y))
                return true;
            if(x is null || y is null)
                return false;

            return Equals(x.AddMethod, y.AddMethod)
                && SequenceEqual(x.Arguments, y.Arguments, expressionComparer);
        }
    }

    private sealed class MemberBindingEqualityComparer(
        IEqualityComparer<Expression?> expressionComparer,
        IEqualityComparer<ElementInit?> elementInitComparer
    )
        : IEqualityComparer<MemberBinding?>
    {
        public int GetHashCode(MemberBinding obj) {
            if(obj is null)
                return 0;

            var hash = new HashCode();
            hash.Add(obj.BindingType);
            hash.Add(obj.Member);

            switch(obj) {
                case MemberAssignment assignment:
                    hash.Add(assignment.Expression, expressionComparer);
                    return hash.ToHashCode();

                case MemberMemberBinding memberBinding:
                    SequenceHash(ref hash, memberBinding.Bindings, this);
                    return hash.ToHashCode();

                case MemberListBinding listBinding:
                    SequenceHash(ref hash, listBinding.Initializers, elementInitComparer);
                    return hash.ToHashCode();

                default:
                    throw UnsupportedMemberBindingException(obj);
            }
        }

        public bool Equals(MemberBinding? x, MemberBinding? y) {
            if(ReferenceEquals(x, y))
                return true;
            if(x is null || y is null)
                return false;

            if(x.BindingType != y.BindingType)
                return false;
            if(!Equals(x.Member, y.Member))
                return false;

            return x.BindingType switch {
                MemberBindingType.Assignment =>
                    expressionComparer.Equals(((MemberAssignment)x).Expression, ((MemberAssignment)y).Expression),

                MemberBindingType.MemberBinding =>
                    SequenceEqual(((MemberMemberBinding)x).Bindings, ((MemberMemberBinding)y).Bindings, this),

                MemberBindingType.ListBinding =>
                    SequenceEqual(((MemberListBinding)x).Initializers, ((MemberListBinding)y).Initializers, elementInitComparer),

                _ => throw UnsupportedMemberBindingException(x)
            };
        }
    }

    private sealed class SwitchCaseEqualityComparer(IEqualityComparer<Expression?> expressionComparer)
        : IEqualityComparer<SwitchCase?>
    {
        public int GetHashCode(SwitchCase obj) {
            if(obj is null)
                return 0;

            var hash = new HashCode();
            hash.Add(obj.Body, expressionComparer);
            SequenceHash(ref hash, obj.TestValues, expressionComparer);
            return hash.ToHashCode();
        }

        public bool Equals(SwitchCase? x, SwitchCase? y) {
            if(ReferenceEquals(x, y))
                return true;
            if(x is null || y is null)
                return false;

            return expressionComparer.Equals(x.Body, y.Body)
                && SequenceEqual(x.TestValues, y.TestValues, expressionComparer);
        }
    }
}
