using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public sealed partial class InterpolatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedTree> {
    private readonly InterpolationAnalysisContext _context;
    private readonly InterpolatedTreeBuilder _builder;
    private ImmutableHashSet<string> _interpolatableIdentifiers;
    private QueryContext _queryContext;

    public InterpolatedSyntaxVisitor(InterpolationAnalysisContext context) {
        _context = context;
        _builder = context.TreeBuilder;
        _queryContext = QueryContext.Create(this);
        _interpolatableIdentifiers = ImmutableHashSet.Create<string>(IdentifierEqualityComparer.Instance);
    }
    
    private void AddInterpolatableIdentifier(string identifier) {
        _interpolatableIdentifiers = _interpolatableIdentifiers.Add(identifier);
    }

    public InterpolatedTree Apply(LambdaExpressionSyntax lambda) {
        // Register the lambda parameters as interpolatable identifiers, less the initial interpolation
        // context parameters
        var parameters = GetLambdaParameters(lambda);
        for(var i = 1; i < parameters.Count; i++)
            AddInterpolatableIdentifier(parameters[i].Identifier.ValueText);
            
        CurrentExpr = new ExpressionBinding(
            parent: default,
            visitor: this,
            identifier: "__e0",
            binding: InterpolatedTree.Interpolate($"{_context.ExpressionParameter.Name}.{nameof(LambdaExpression.Body)}"),
            expressionType: default
        );
        
        return CurrentExpr.WithValue(Visit(lambda.Body));
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

        return node switch {
            null => base.Visit(node)!,
            not null => ApplyImplicitConversion(node)
        };
    }

    public override InterpolatedTree DefaultVisit(SyntaxNode node) {
        return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
    }

    /// <summary>
    /// Emits a convert expression wrapping the provided <paramref name="tree"/> for any implicit
    /// conversion associated with the provided <paramref name="node"/>.
    /// </summary>
    private InterpolatedTree ApplyImplicitConversion(SyntaxNode node) {
        var conversion = _context.SemanticModel.GetConversion(node);
        switch(conversion) {
            case not { Exists: true, IsImplicit: true }:
                return base.Visit(node)!;

            case { IsBoxing: true }:
            case { IsConditionalExpression: true }:
            case { IsConstantExpression: true }:
            case { IsDefaultLiteral: true }:
            case { IsEnumeration: true }:
            case { IsNullable: true }:
            case { IsNumeric: true }:
            case { IsUserDefined: true }:
                break;

            default:
                return base.Visit(node)!;
        }

        var typeInfo = _context.SemanticModel.GetTypeInfo(node);
        if(typeInfo.ConvertedType is null)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        if(SymbolEqualityComparer.IncludeNullability.Equals(typeInfo.ConvertedType, typeInfo.Type))
            return base.Visit(node)!;

        // In the case of a user-defined conversion, information about the method is provided, however
        // it does not appear to be necessary to use this information despite the fact that there is
        // an overload of Expression.Convert which exists specifically to handle this situation.
        // Conveniently for the moment this saves us from having to deal with resolving a nameless,
        // possibly generic method.
        SetBoundType(typeof(UnaryExpression));
        return _builder.CreateExpression(
            SyntaxHelpers.InCheckedContext(node, _context.SemanticModel) switch {
                true => nameof(Expression.ConvertChecked),
                false => nameof(Expression.Convert)
            },
            [
                Bind($"{nameof(UnaryExpression.Operand)}").WithValue(base.Visit(node)!),
                BindValue($"{nameof(UnaryExpression.Type)}"),
                BindValue($"{nameof(UnaryExpression.Method)}"),
            ]
        );
    }

    private InterpolatedTree VisitEvaluatedSyntax(SyntaxNode node) =>
        new EvaluatedSyntaxVisitor(_context, _interpolatableIdentifiers).Visit(node);

    public override InterpolatedTree VisitIdentifierName(IdentifierNameSyntax node) {
        if(!_interpolatableIdentifiers.Contains(node.Identifier.Text))
            return _context.Diagnostics.ClosureOverScopeReference(node);

        // Return the current expression, which references the expression to which the identifier
        // is bound (typically a ParameterExpression, but may also be a MemberExpression in the
        // case of an identifier bound in a query expression)
        SetBoundType(typeof(Expression));
        return CurrentExpr.Identifier;
    }

    public override InterpolatedTree VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) {
        SetBoundType(typeof(NewArrayExpression));
        return _builder.CreateExpression(nameof(Expression.NewArrayInit), [
            BindValue($"{nameof(NewArrayExpression.Type)}.{nameof(Type.GetElementType)}()!"),
            _builder.CreateExpressionArray(node.Initializer.Expressions.SelectEager(
                (expr, i) => Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(expr))
            ))
        ]);
    }

    public override InterpolatedTree VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) {
        SetBoundType(typeof(NewArrayExpression));
        
        // If the node has an initializer, then the array dimensions are required to be constants
        // and the expression is a NewArrayInit because the length is effectively implied by the
        // initializer
        if(node.Initializer is not null)
            return _builder.CreateExpression(nameof(Expression.NewArrayInit), [
                BindValue($"{nameof(NewArrayExpression.Type)}.{nameof(Type.GetElementType)}()!"),
                _builder.CreateExpressionArray(node.Initializer.Expressions.SelectEager(
                    (expr, i) => Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(expr))
                ))
            ]);
            
        // Otherwise the array dimensions are not required to be constants, and the expression is a
        // NewArrayBounds. Note that only the first rank specifier of the array can contain dimensions
        // (if there are multiple specifiers it is a nested array type).
        return _builder.CreateExpression(nameof(Expression.NewArrayBounds), [
            BindValue($"{nameof(NewArrayExpression.Type)}.{nameof(Type.GetElementType)}()!"),
            _builder.CreateExpressionArray(node.Type.RankSpecifiers[0].Sizes.SelectEager(
                (size, i) => Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(size))
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
                SetBoundType(typeof(MethodCallExpression));
                return _builder.CreateExpression(nameof(Expression.Call),
                    BindValue($"{nameof(MethodCallExpression.Method)}"),
                    _builder.CreateExpressionArray([
                        Bind($"{nameof(MethodCallExpression.Arguments)}[0]").WithValue(Visit(node.Expression)),
                        ..node.ArgumentList.Arguments.SelectEager(
                            (arg, i) => Bind($"{nameof(MethodCallExpression.Arguments)}[{i + 1}]").WithValue(Visit(arg))
                        )
                    ])
                );

            case IMethodSymbol { IsStatic: true }:
                SetBoundType(typeof(MethodCallExpression));
                return _builder.CreateExpression(nameof(Expression.Call),
                    BindValue($"{nameof(MethodCallExpression.Method)}"),
                    _builder.CreateExpressionArray(node.ArgumentList.Arguments.SelectEager(
                        (arg, i) => Bind($"{nameof(MethodCallExpression.Arguments)}[{i}]").WithValue(Visit(arg))
                    ))
                );

            case IMethodSymbol:
                SetBoundType(typeof(MethodCallExpression));
                return _builder.CreateExpression(nameof(Expression.Call),
                    Bind($"{nameof(MethodCallExpression.Object)}").WithValue(Visit(node.Expression)),
                    BindValue($"{nameof(MethodCallExpression.Method)}"),
                    _builder.CreateExpressionArray(node.ArgumentList.Arguments.SelectEager(
                        (arg, i) => Bind($"{nameof(MethodCallExpression.Arguments)}[{i}]").WithValue(Visit(arg))
                    ))
                );

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
    }

    public override InterpolatedTree VisitArgument(ArgumentSyntax node) =>
        Visit(node.Expression);

    public override InterpolatedTree VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        var symbol = _context.SemanticModel.GetSymbolInfo(node).Symbol;
        if(_context.IsInterpolationDataAccess(symbol))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        return VisitMemberAccess(node, symbol);
    }

    private InterpolatedTree VisitMemberAccess(MemberAccessExpressionSyntax node, ISymbol? symbol) {
        switch(symbol) {
            case IFieldSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true }:
                SetBoundType(typeof(MemberExpression));
                return _builder.CreateExpression(nameof(Expression.MakeMemberAccess),
                    _builder.CreateDefaultValue(_context.TypeSymbols.Expression.WithNullableAnnotation(NullableAnnotation.Annotated)),
                    BindValue($"{nameof(MemberExpression.Member)}")
                );

            case IFieldSymbol or IPropertySymbol:
                SetBoundType(typeof(MemberExpression));
                return _builder.CreateExpression(nameof(Expression.MakeMemberAccess),
                    Bind($"{nameof(MemberExpression.Expression)}!").WithValue(Visit(node.Expression)),
                    BindValue($"{nameof(MemberExpression.Member)}")
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
        SetBoundType(typeof(UnaryExpression));
        return _builder.CreateExpression(
            SyntaxHelpers.InCheckedContext(node, _context.SemanticModel) switch {
                true => nameof(Expression.ConvertChecked),
                false => nameof(Expression.Convert)
            },
            [
                Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(node.Expression)),
                BindValue($"{nameof(UnaryExpression.Type)}"),
                BindValue($"{nameof(UnaryExpression.Method)}"),
            ]
        );
    }

    public override InterpolatedTree VisitDefaultExpression(DefaultExpressionSyntax node) {
        SetBoundType(typeof(DefaultExpression));
        return _builder.CreateExpression(nameof(Expression.Default), [
            BindValue($"{nameof(DefaultExpression.Type)}")
        ]);
    }

    public override InterpolatedTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) {
        SetBoundType(typeof(NewExpression));
        return _builder.CreateExpression(nameof(Expression.New), [
            BindValue($"{nameof(NewExpression.Constructor)}!"),
            _builder.CreateExpressionArray(node.Initializers.SelectEager(
                (init, i) => Bind($"{nameof(NewExpression.Arguments)}[{i}]").WithValue(Visit(init.Expression))
            )),
            BindValue($"{nameof(NewExpression.Members)}")
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
                SetBoundType(typeof(MemberInitExpression));
                return _builder.CreateExpression(nameof(Expression.MemberInit), [
                    Bind($"{nameof(MemberInitExpression.NewExpression)}").WithValue(CreateNewTree(node)),
                    Visit(node.Initializer)
                ]);
                
            case SyntaxKind.CollectionInitializerExpression:
                SetBoundType(typeof(ListInitExpression));
                return _builder.CreateExpression(nameof(Expression.ListInit), [
                    Bind($"{nameof(ListInitExpression.NewExpression)}").WithValue(CreateNewTree(node)),
                    Visit(node.Initializer)
                ]);
                
            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
        
        InterpolatedTree CreateNewTree(BaseObjectCreationExpressionSyntax node) {
            SetBoundType(typeof(NewExpression));
            return _builder.CreateExpression(nameof(Expression.New), [
                BindValue($"{nameof(NewExpression.Constructor)}!"),
                node.ArgumentList switch {
                    null => _builder.CreateExpressionArray([]),
                    not null => _builder.CreateExpressionArray(node.ArgumentList.Arguments.SelectEager(
                        (arg, i) => Bind($"{nameof(NewExpression.Arguments)}[{i}]").WithValue(Visit(arg.Expression))
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
                return InterpolatedTree.Concat(
                    InterpolatedTree.Verbatim("new global::System.Linq.Expressions.MemberBinding[] "),
                    InterpolatedTree.Initializer(node.Expressions.SelectEager(
                        (init, i) => Bind($"{nameof(MemberInitExpression.Bindings)}[{i}]")
                        .WithValue(VisitObjectInitializerElement(init))
                    ))
                );
                
            case SyntaxKind.CollectionInitializerExpression:
                return InterpolatedTree.Concat(
                    InterpolatedTree.Verbatim("new global::System.Linq.Expressions.ElementInit[] "),
                    InterpolatedTree.Initializer(node.Expressions.SelectEager(
                        (init, i) => Bind($"{nameof(ListInitExpression.Initializers)}[{i}]")
                        .WithValue(VisitCollectionInitializerElement(init))
                    ))
                );
                
            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
    }

    private InterpolatedTree VisitObjectInitializerElement(ExpressionSyntax node) {
        switch(node) {
            case AssignmentExpressionSyntax { Left: IdentifierNameSyntax } assignment:
                SetBoundType(typeof(MemberAssignment));
                return _builder.CreateExpression(nameof(Expression.Bind), [
                    BindValue($"{nameof(MemberAssignment.Member)}"),
                    Bind($"{nameof(MemberAssignment.Expression)}").WithValue(Visit(assignment.Right))
                ]);

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
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
        SetBoundType(typeof(ElementInit));
        return _builder.CreateExpression(nameof(Expression.ElementInit), [
            BindValue($"{nameof(ElementInit.AddMethod)}"),
            _builder.CreateExpressionArray(argumentExpressions.SelectEager(
                (arg, i) => Bind($"{nameof(ElementInit.Arguments)}[{i}]").WithValue(Visit(arg))
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
        if(!TypeSymbolHelpers.IsSubtype(typeSymbol, _context.TypeSymbols.Expression))
            return VisitLambdaExpressionUnquoted(node);
        
        SetBoundType(typeof(UnaryExpression));
        return _builder.CreateExpression(nameof(Expression.Quote), [
            Bind($"{nameof(UnaryExpression.Operand)}")
            .WithValue(VisitLambdaExpressionUnquoted(node))
        ]);
    }
    
    private InterpolatedTree VisitLambdaExpressionUnquoted(LambdaExpressionSyntax node) {
        var snapshot = _interpolatableIdentifiers;
        try {
            foreach(var parameter in GetLambdaParameters(node))
                AddInterpolatableIdentifier(parameter.Identifier.ValueText);
            
            SetBoundType(typeof(LambdaExpression));
            return _builder.CreateExpression(nameof(Expression.Lambda), [
                Bind($"{nameof(LambdaExpression.Body)}").WithValue(Visit(node.Body)),
                BindValue($"{nameof(LambdaExpression.Parameters)}")
            ]);
        } finally {
            _interpolatableIdentifiers = snapshot;
        }
    }

    public override InterpolatedTree VisitConditionalExpression(ConditionalExpressionSyntax node) {
        SetBoundType(typeof(ConditionalExpression));
        return _builder.CreateExpression(nameof(Expression.Condition),
            Bind($"{nameof(ConditionalExpression.Test)}").WithValue(Visit(node.Condition)),
            Bind($"{nameof(ConditionalExpression.IfTrue)}").WithValue(Visit(node.WhenTrue)),
            Bind($"{nameof(ConditionalExpression.IfFalse)}").WithValue(Visit(node.WhenFalse)),
            BindValue($"{nameof(ConditionalExpression.Type)}")
        );
    }

    public override InterpolatedTree VisitBinaryExpression(BinaryExpressionSyntax node) {
        if(TryVisitBinarySpecialExpression(node, out var special))
            return special;

        SetBoundType(typeof(BinaryExpression));
        return _builder.CreateExpression(nameof(Expression.MakeBinary),
            BindValue($"{nameof(BinaryExpression.NodeType)}"),
            Bind($"{nameof(BinaryExpression.Left)}").WithValue(Visit(node.Left)),
            Bind($"{nameof(BinaryExpression.Right)}").WithValue(Visit(node.Right)),
            BindValue($"{nameof(BinaryExpression.IsLiftedToNull)}"),
            BindValue($"{nameof(BinaryExpression.Method)}")
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
        SetBoundType(typeof(UnaryExpression));
        return _builder.CreateExpression(nameof(Expression.TypeAs), [
            Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(node.Left)),
            BindValue($"{nameof(UnaryExpression.Type)}")
        ]);
    }

    private InterpolatedTree VisitBinaryIsExpression(BinaryExpressionSyntax node) {
        SetBoundType(typeof(TypeBinaryExpression));
        return _builder.CreateExpression(nameof(Expression.TypeIs), [
            Bind($"{nameof(TypeBinaryExpression.Expression)}").WithValue(Visit(node.Left)),
            BindValue($"{nameof(TypeBinaryExpression.TypeOperand)}")
        ]);
    }

    public override InterpolatedTree VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) =>
        VisitUnaryExpression(node, node.Operand);

    public override InterpolatedTree VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) =>
        VisitUnaryExpression(node, node.Operand);
        
    private InterpolatedTree VisitUnaryExpression(ExpressionSyntax node, ExpressionSyntax operand) {
        if(TryVisitUnarySpecialExpression(node, operand, out var special))
            return special;
    
        SetBoundType(typeof(UnaryExpression));
        return _builder.CreateExpression(nameof(Expression.MakeUnary),
            BindValue($"{nameof(UnaryExpression.NodeType)}"),
            Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(operand)),
            BindValue($"{nameof(UnaryExpression.Type)}"),
            BindValue($"{nameof(UnaryExpression.Method)}")
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
        SetBoundType(typeof(ConstantExpression));
        return CurrentExpr.Identifier;
    }
}
