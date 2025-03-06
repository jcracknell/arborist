using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public sealed partial class InterpolatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedTree> {
    private readonly InterpolationAnalysisContext _context;
    private readonly InterpolatedTreeBuilder _builder;
    private QueryContext _queryContext;

    /// <summary>
    /// The IInterpolationContext identifier, which cannot be referenced in the interpolated
    /// expression tree. May be empty in the event that the identifier is rebound/shadowed.
    /// </summary>
    private ImmutableHashSet<string> _contextIdentifier;

    /// <summary>
    /// Identifiers introduced by the interpolated expression (typically as lambda parameters)
    /// which cannot be referenced in an evaluated expression.
    /// </summary>
    private ImmutableHashSet<string> _interpolatedIdentifiers;

    public InterpolatedSyntaxVisitor(InterpolationAnalysisContext context) {
        _context = context;
        _builder = context.TreeBuilder;
        _queryContext = QueryContext.Create(this);
        _contextIdentifier = ImmutableHashSet.Create<string>(IdentifierEqualityComparer.Instance);
        _interpolatedIdentifiers = ImmutableHashSet.Create<string>(IdentifierEqualityComparer.Instance);
    }

    public bool SplicesFound { get; private set; }

    private void AddInterpolatedIdentifier(string identifier) {
        // Handle rebinding/shadowing of the context parameter
        _contextIdentifier = _contextIdentifier.Remove(identifier);
        _interpolatedIdentifiers = _interpolatedIdentifiers.Add(identifier);
    }

    private IdentifiersSnapshot CreateIdentifiersSnapshot() =>
        new IdentifiersSnapshot(
            visitor: this,
            contextSnapshot: _contextIdentifier,
            interpolatedSnapshot: _interpolatedIdentifiers
        );

    private readonly struct IdentifiersSnapshot(
        InterpolatedSyntaxVisitor visitor,
        ImmutableHashSet<string> contextSnapshot,
        ImmutableHashSet<string> interpolatedSnapshot
    ) : IDisposable {
        void IDisposable.Dispose() {
            Restore();
        }

        public void Restore() {
            visitor._contextIdentifier = contextSnapshot;
            visitor._interpolatedIdentifiers = interpolatedSnapshot;
        }
    }

    public InterpolatedTree Apply(LambdaExpressionSyntax lambda) {
        var parameters = GetLambdaParameters(lambda);

        // Register the interpolation context parameter as a forbidden identifier
        _contextIdentifier = _contextIdentifier.Add(parameters[0].Identifier.ValueText);
        // Register all of the parameters as interpolated identifiers which cannot be referenced in an
        // evaluated expression. We don't use the helper method here to preserve the context identifier.
        _interpolatedIdentifiers = _interpolatedIdentifiers.Union(parameters.Select(p => p.Identifier.ValueText));

        var rootExpr = CurrentExpr = new InterpolatedExpressionBinding(
            parent: default,
            visitor: this,
            binding: InterpolatedTree.Interpolate($"{_context.ExpressionParameter.Name}.{nameof(LambdaExpression.Body)}"),
            expressionType: default
        );

        var result = rootExpr.WithValue(Visit(lambda.Body));

        SplicesFound = rootExpr.IsMarked;
        return result;
    }

    private IReadOnlyList<ParameterSyntax> GetLambdaParameters(LambdaExpressionSyntax node) =>
        node switch {
            SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter },
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters,
            _ => throw new NotImplementedException()
        };

    [return: NotNullIfNotNull("node")]
    public override InterpolatedTree? Visit(SyntaxNode? node) {
        // Check for cancellation every time we visit (iterate) over a node
        _context.CancellationToken.ThrowIfCancellationRequested();

        return node is null ? base.Visit(node)! : ApplyImplicitConversion(node);
    }

    public override InterpolatedTree DefaultVisit(SyntaxNode node) {
        return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
    }

    /// <summary>
    /// Emits a convert expression wrapping the provided <paramref name="tree"/> for any implicit
    /// conversion associated with the provided <paramref name="node"/>.
    /// </summary>
    private InterpolatedTree ApplyImplicitConversion(SyntaxNode node) {
        if(!SyntaxHelpers.HasImplicitConversion(node, _context.SemanticModel))
            return base.Visit(node)!;

        // In the case of a user-defined conversion, information about the method is provided, however
        // it does not appear to be necessary to use this information despite the fact that there is
        // an overload of Expression.Convert which exists specifically to handle this situation.
        // Conveniently for the moment this saves us from having to deal with resolving a nameless,
        // possibly generic method.
        CurrentExpr.SetType(typeof(UnaryExpression));
        return _builder.CreateExpression(
            SyntaxHelpers.InCheckedContext(node, _context.SemanticModel) switch {
                true => nameof(Expression.ConvertChecked),
                false => nameof(Expression.Convert)
            },
            [
                CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(base.Visit(node)!),
                CurrentExpr.BindValue($"{nameof(UnaryExpression.Type)}"),
                CurrentExpr.BindValue($"{nameof(UnaryExpression.Method)}"),
            ]
        );
    }

    private InterpolatedTree VisitEvaluatedSyntax(SyntaxNode node) {
        CurrentExpr.Mark();
        return new EvaluatedSyntaxVisitor(_context, CurrentExpr, _interpolatedIdentifiers).Apply(node);
    }

    public override InterpolatedTree VisitThisExpression(ThisExpressionSyntax node) {
        CurrentExpr.SetType(typeof(Expression));
        return CurrentExpr.Identifier;
    }

    public override InterpolatedTree VisitIdentifierName(IdentifierNameSyntax node) {
        // The only identifier which cannot be referenced within the interpolated expression tree
        // is the one referencing the interpolation context, which does not exist in the result
        // expression.
        if(_contextIdentifier.Contains(node.Identifier.ValueText))
            return _context.Diagnostics.InterpolationContextReference(node);

        // Return the current expression, which references the expression to which the identifier
        // is bound (typically a ParameterExpression, but may also be a MemberExpression in the
        // case of an identifier bound in a query expression)
        CurrentExpr.SetType(typeof(Expression));
        return CurrentExpr.Identifier;
    }

    public override InterpolatedTree VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) {
        CurrentExpr.SetType(typeof(NewArrayExpression));
        return _builder.CreateExpression(nameof(Expression.NewArrayInit), [
            CurrentExpr.BindValue($"{nameof(NewArrayExpression.Type)}.{nameof(Type.GetElementType)}()!"),
            _builder.CreateExpressionArray(node.Initializer.Expressions.SelectEager(
                (expr, i) => CurrentExpr.Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(expr))
            ))
        ]);
    }

    public override InterpolatedTree VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) {
        CurrentExpr.SetType(typeof(NewArrayExpression));

        // If the node has an initializer, then the array dimensions are required to be constants
        // and the expression is a NewArrayInit because the length is effectively implied by the
        // initializer
        if(node.Initializer is not null)
            return _builder.CreateExpression(nameof(Expression.NewArrayInit), [
                CurrentExpr.BindValue($"{nameof(NewArrayExpression.Type)}.{nameof(Type.GetElementType)}()!"),
                _builder.CreateExpressionArray(node.Initializer.Expressions.SelectEager(
                    (expr, i) => CurrentExpr.Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(expr))
                ))
            ]);

        // Otherwise the array dimensions are not required to be constants, and the expression is a
        // NewArrayBounds. Note that only the first rank specifier of the array can contain dimensions
        // (if there are multiple specifiers it is a nested array type).
        return _builder.CreateExpression(nameof(Expression.NewArrayBounds), [
            CurrentExpr.BindValue($"{nameof(NewArrayExpression.Type)}.{nameof(Type.GetElementType)}()!"),
            _builder.CreateExpressionArray(node.Type.RankSpecifiers[0].Sizes.SelectEager(
                (size, i) => CurrentExpr.Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(size))
            ))
        ]);
    }

    public override InterpolatedTree VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(TryGetSplicingMethod(node, out var spliceMethod))
            return VisitSplicingInvocation(node, spliceMethod);

        return VisitInvocation(node);
    }

    private InterpolatedTree VisitInvocation(InvocationExpressionSyntax node) {
        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IMethodSymbol { ReducedFrom: {} } method:
                CurrentExpr.SetType(typeof(MethodCallExpression));
                return _builder.CreateExpression(nameof(Expression.Call),
                    CurrentExpr.BindValue($"{nameof(MethodCallExpression.Method)}"),
                    _builder.CreateExpressionArray([
                        CurrentExpr.Bind($"{nameof(MethodCallExpression.Arguments)}[0]").WithValue(Visit(node.Expression)),
                        ..node.ArgumentList.Arguments.SelectEager(
                            (arg, i) => CurrentExpr.Bind($"{nameof(MethodCallExpression.Arguments)}[{i + 1}]").WithValue(Visit(arg))
                        )
                    ])
                );

            case IMethodSymbol { IsStatic: true }:
                CurrentExpr.SetType(typeof(MethodCallExpression));
                return _builder.CreateExpression(nameof(Expression.Call),
                    CurrentExpr.BindValue($"{nameof(MethodCallExpression.Method)}"),
                    _builder.CreateExpressionArray(node.ArgumentList.Arguments.SelectEager(
                        (arg, i) => CurrentExpr.Bind($"{nameof(MethodCallExpression.Arguments)}[{i}]").WithValue(Visit(arg))
                    ))
                );

            case IMethodSymbol:
                CurrentExpr.SetType(typeof(MethodCallExpression));
                return _builder.CreateExpression(nameof(Expression.Call),
                    CurrentExpr.Bind($"{nameof(MethodCallExpression.Object)}!").WithValue(Visit(node.Expression)),
                    CurrentExpr.BindValue($"{nameof(MethodCallExpression.Method)}"),
                    _builder.CreateExpressionArray(node.ArgumentList.Arguments.SelectEager(
                        (arg, i) => CurrentExpr.Bind($"{nameof(MethodCallExpression.Arguments)}[{i}]").WithValue(Visit(arg))
                    ))
                );

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
    }

    public override InterpolatedTree VisitArgument(ArgumentSyntax node) =>
        Visit(node.Expression);

    public override InterpolatedTree VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IFieldSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true }:
                CurrentExpr.SetType(typeof(MemberExpression));
                return CurrentExpr.Identifier;

            case IFieldSymbol or IPropertySymbol:
                CurrentExpr.SetType(typeof(MemberExpression));
                return _builder.CreateExpression(nameof(Expression.MakeMemberAccess),
                    CurrentExpr.Bind($"{nameof(MemberExpression.Expression)}!").WithValue(Visit(node.Expression)),
                    CurrentExpr.BindValue($"{nameof(MemberExpression.Member)}")
                );

            case IMethodSymbol:
                return Visit(node.Expression);

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
    }

    public override InterpolatedTree VisitCheckedExpression(CheckedExpressionSyntax node) {
        // Checked expressions do not appear in the resulting expression tree, however they
        // alter the expressions produced for their decendants.
        return Visit(node.Expression);
    }

    public override InterpolatedTree VisitCastExpression(CastExpressionSyntax node) {
        CurrentExpr.SetType(typeof(UnaryExpression));
        return _builder.CreateExpression(
            SyntaxHelpers.InCheckedContext(node, _context.SemanticModel) switch {
                true => nameof(Expression.ConvertChecked),
                false => nameof(Expression.Convert)
            },
            [
                CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(node.Expression)),
                CurrentExpr.BindValue($"{nameof(UnaryExpression.Type)}"),
                CurrentExpr.BindValue($"{nameof(UnaryExpression.Method)}"),
            ]
        );
    }

    public override InterpolatedTree VisitDefaultExpression(DefaultExpressionSyntax node) {
        CurrentExpr.SetType(typeof(DefaultExpression));
        return _builder.CreateExpression(nameof(Expression.Default), [
            CurrentExpr.BindValue($"{nameof(DefaultExpression.Type)}")
        ]);
    }

    public override InterpolatedTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) {
        CurrentExpr.SetType(typeof(NewExpression));
        return _builder.CreateExpression(nameof(Expression.New), [
            CurrentExpr.BindValue($"{nameof(NewExpression.Constructor)}!"),
            _builder.CreateExpressionArray(node.Initializers.SelectEager(
                (init, i) => CurrentExpr.Bind($"{nameof(NewExpression.Arguments)}[{i}]").WithValue(Visit(init.Expression))
            )),
            CurrentExpr.BindValue($"{nameof(NewExpression.Members)}")
        ]);
    }

    public override InterpolatedTree VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    public override InterpolatedTree VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    private InterpolatedTree VisitBaseObjectCreationExpression(BaseObjectCreationExpressionSyntax node) {
        switch(node.Initializer?.Kind()) {
            case null:
                return CreateNewTree(node);

            case SyntaxKind.ObjectInitializerExpression:
                CurrentExpr.SetType(typeof(MemberInitExpression));
                return _builder.CreateExpression(nameof(Expression.MemberInit), [
                    CurrentExpr.Bind($"{nameof(MemberInitExpression.NewExpression)}").WithValue(CreateNewTree(node)),
                    Visit(node.Initializer)
                ]);

            case SyntaxKind.CollectionInitializerExpression:
                CurrentExpr.SetType(typeof(ListInitExpression));
                return _builder.CreateExpression(nameof(Expression.ListInit), [
                    CurrentExpr.Bind($"{nameof(ListInitExpression.NewExpression)}").WithValue(CreateNewTree(node)),
                    Visit(node.Initializer)
                ]);

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }

        InterpolatedTree CreateNewTree(BaseObjectCreationExpressionSyntax node) {
            CurrentExpr.SetType(typeof(NewExpression));
            return _builder.CreateExpression(nameof(Expression.New), [
                CurrentExpr.BindValue($"{nameof(NewExpression.Constructor)}!"),
                node.ArgumentList switch {
                    null => _builder.CreateExpressionArray([]),
                    not null => _builder.CreateExpressionArray(node.ArgumentList.Arguments.SelectEager(
                        (arg, i) => CurrentExpr.Bind($"{nameof(NewExpression.Arguments)}[{i}]").WithValue(Visit(arg.Expression))
                    ))
                }
            ]);
        }
    }

    public override InterpolatedTree VisitInitializerExpression(InitializerExpressionSyntax node) {
        // This is a pain in the butt because from a syntactical standpoint an initializer is a "bracketed list
        // of things" (e.g. a collection initializer is a bracketed list of bracketed lists), but we care very
        // much about the context in which the bracketed list and its elements occur.
        switch(node.Kind()) {
            case SyntaxKind.ObjectInitializerExpression:
                // N.B. MemberInitExpression and MemberMemberBinding both have the Bindings property, so it
                // does not matter which case this is.
                return InterpolatedTree.Concat(
                    InterpolatedTree.Verbatim("new global::System.Linq.Expressions.MemberBinding[] "),
                    InterpolatedTree.Initializer(node.Expressions.SelectEager(
                        (init, i) => CurrentExpr.Bind($"{nameof(MemberInitExpression.Bindings)}[{i}]")
                        .WithValue(VisitObjectInitializerElement(init))
                    ))
                );

            case SyntaxKind.CollectionInitializerExpression:
                // N.B. ListInitExpression and MemberListBinding both have the Initializers property, so it
                // does not matter which case this is.
                return InterpolatedTree.Concat(
                    InterpolatedTree.Verbatim("new global::System.Linq.Expressions.ElementInit[] "),
                    InterpolatedTree.Initializer(node.Expressions.SelectEager(
                        (init, i) => CurrentExpr.Bind($"{nameof(ListInitExpression.Initializers)}[{i}]")
                        .WithValue(VisitCollectionInitializerElement(init))
                    ))
                );

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
    }

    private InterpolatedTree VisitObjectInitializerElement(ExpressionSyntax node) {
        if(node is not AssignmentExpressionSyntax { Left: IdentifierNameSyntax } assignment)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        // Handle the case of a nested object/collection initializer
        if(assignment.Right is InitializerExpressionSyntax initializer) switch(initializer.Kind()) {
            case SyntaxKind.ObjectInitializerExpression:
                CurrentExpr.SetType(typeof(MemberMemberBinding));
                return _builder.CreateExpression(nameof(Expression.MemberBind), [
                    CurrentExpr.BindValue($"{nameof(MemberMemberBinding.Member)}"),
                    VisitInitializerExpression(initializer)
                ]);

            case SyntaxKind.CollectionInitializerExpression:
                CurrentExpr.SetType(typeof(MemberListBinding));
                return _builder.CreateExpression(nameof(Expression.ListBind), [
                    CurrentExpr.BindValue($"{nameof(MemberListBinding.Member)}"),
                    VisitInitializerExpression(initializer)
                ]);

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }

        CurrentExpr.SetType(typeof(MemberAssignment));
        return _builder.CreateExpression(nameof(Expression.Bind), [
            CurrentExpr.BindValue($"{nameof(MemberAssignment.Member)}"),
            CurrentExpr.Bind($"{nameof(MemberAssignment.Expression)}").WithValue(Visit(assignment.Right))
        ]);
    }

    private InterpolatedTree VisitCollectionInitializerElement(ExpressionSyntax node) =>
        node switch {
            InitializerExpressionSyntax initializer =>
                VisitCollectionInitializerElement(initializer, initializer.Expressions),
            _ => VisitCollectionInitializerElement(node, new[] { node })
        };

    private InterpolatedTree VisitCollectionInitializerElement(
        ExpressionSyntax node,
        IReadOnlyList<ExpressionSyntax> argumentExpressions
    ) {
        CurrentExpr.SetType(typeof(ElementInit));
        return _builder.CreateExpression(nameof(Expression.ElementInit), [
            CurrentExpr.BindValue($"{nameof(ElementInit.AddMethod)}"),
            _builder.CreateExpressionArray(argumentExpressions.SelectEager(
                (arg, i) => CurrentExpr.Bind($"{nameof(ElementInit.Arguments)}[{i}]").WithValue(Visit(arg))
            ))
        ]);
    }

    public override InterpolatedTree VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) =>
        Visit(node.Expression)!;

    public override InterpolatedTree VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) =>
        VisitLambdaExpression(node);

    public override InterpolatedTree VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) =>
        VisitLambdaExpression(node);

    private InterpolatedTree VisitLambdaExpression(LambdaExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).ConvertedType is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        // If the lambda is an expression, then it is enclosed in a quoted UnaryExpression
        if(!SymbolHelpers.IsSubtype(typeSymbol, _context.TypeSymbols.Expression))
            return VisitLambdaExpressionCore(node);

        CurrentExpr.SetType(typeof(UnaryExpression));
        return _builder.CreateExpression(nameof(Expression.Quote), [
            CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}")
            .WithValue(VisitLambdaExpressionCore(node))
        ]);
    }

    private InterpolatedTree VisitLambdaExpressionCore(LambdaExpressionSyntax node) {
        using var snapshot = CreateIdentifiersSnapshot();

        foreach(var parameter in GetLambdaParameters(node))
            AddInterpolatedIdentifier(parameter.Identifier.ValueText);

        CurrentExpr.SetType(typeof(LambdaExpression));
        return _builder.CreateExpression(nameof(Expression.Lambda), [
            CurrentExpr.BindValue($"{nameof(LambdaExpression.Type)}"),
            CurrentExpr.Bind($"{nameof(LambdaExpression.Body)}").WithValue(Visit(node.Body)),
            CurrentExpr.BindValue($"{nameof(LambdaExpression.Parameters)}")
        ]);
    }

    public override InterpolatedTree VisitConditionalExpression(ConditionalExpressionSyntax node) {
        CurrentExpr.SetType(typeof(ConditionalExpression));
        return _builder.CreateExpression(nameof(Expression.Condition),
            CurrentExpr.Bind($"{nameof(ConditionalExpression.Test)}").WithValue(Visit(node.Condition)),
            CurrentExpr.Bind($"{nameof(ConditionalExpression.IfTrue)}").WithValue(Visit(node.WhenTrue)),
            CurrentExpr.Bind($"{nameof(ConditionalExpression.IfFalse)}").WithValue(Visit(node.WhenFalse)),
            CurrentExpr.BindValue($"{nameof(ConditionalExpression.Type)}")
        );
    }

    public override InterpolatedTree VisitBinaryExpression(BinaryExpressionSyntax node) {
        if(TryVisitBinarySpecialExpression(node, out var special))
            return special;

        CurrentExpr.SetType(typeof(BinaryExpression));
        return _builder.CreateExpression(nameof(Expression.MakeBinary),
            CurrentExpr.BindValue($"{nameof(BinaryExpression.NodeType)}"),
            CurrentExpr.Bind($"{nameof(BinaryExpression.Left)}").WithValue(Visit(node.Left)),
            CurrentExpr.Bind($"{nameof(BinaryExpression.Right)}").WithValue(Visit(node.Right)),
            CurrentExpr.BindValue($"{nameof(BinaryExpression.IsLiftedToNull)}"),
            CurrentExpr.BindValue($"{nameof(BinaryExpression.Method)}")
        );
    }

    private bool TryVisitBinarySpecialExpression(
        BinaryExpressionSyntax node,
        [NotNullWhen(true)] out InterpolatedTree? result
    ) {
        switch(node.Kind()) {
            case SyntaxKind.AsExpression:
                result = VisitBinaryAsExpression(node);
                return true;
            case SyntaxKind.IsExpression:
                result = VisitBinaryIsExpression(node);
                return true;
            default:
                result = default;
                return false;
        }
    }

    private InterpolatedTree VisitBinaryAsExpression(BinaryExpressionSyntax node) {
        CurrentExpr.SetType(typeof(UnaryExpression));
        return _builder.CreateExpression(nameof(Expression.TypeAs), [
            CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(node.Left)),
            CurrentExpr.BindValue($"{nameof(UnaryExpression.Type)}")
        ]);
    }

    private InterpolatedTree VisitBinaryIsExpression(BinaryExpressionSyntax node) {
        CurrentExpr.SetType(typeof(TypeBinaryExpression));
        return _builder.CreateExpression(nameof(Expression.TypeIs), [
            CurrentExpr.Bind($"{nameof(TypeBinaryExpression.Expression)}").WithValue(Visit(node.Left)),
            CurrentExpr.BindValue($"{nameof(TypeBinaryExpression.TypeOperand)}")
        ]);
    }

    public override InterpolatedTree VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) =>
        VisitUnaryExpression(node, node.Operand);

    public override InterpolatedTree VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) =>
        VisitUnaryExpression(node, node.Operand);

    private InterpolatedTree VisitUnaryExpression(ExpressionSyntax node, ExpressionSyntax operand) {
        if(TryVisitUnarySpecialExpression(node, operand, out var special))
            return special;

        CurrentExpr.SetType(typeof(UnaryExpression));
        return _builder.CreateExpression(nameof(Expression.MakeUnary),
            CurrentExpr.BindValue($"{nameof(UnaryExpression.NodeType)}"),
            CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(operand)),
            CurrentExpr.BindValue($"{nameof(UnaryExpression.Type)}"),
            CurrentExpr.BindValue($"{nameof(UnaryExpression.Method)}")
        );
    }

    private bool TryVisitUnarySpecialExpression(
        ExpressionSyntax node,
        ExpressionSyntax operand,
        [NotNullWhen(true)] out InterpolatedTree? result
    ) {
        switch(node.Kind()) {
            case SyntaxKind.SuppressNullableWarningExpression:
                // The null forgiving operator does nothing, and is omitted from the resulting expression tree
                result = Visit(operand);
                return true;
            default:
                result = default;
                return false;
        }
    }

    public override InterpolatedTree VisitLiteralExpression(LiteralExpressionSyntax node) {
        CurrentExpr.SetType(typeof(ConstantExpression));
        return CurrentExpr.Identifier;
    }
}
