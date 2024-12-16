using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public sealed partial class InterpolatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedTree> {
    private readonly InterpolatorInvocationContext _context;
    private readonly InterpolatedExpressionBuilder _builder;
    private ImmutableDictionary<string, InterpolatedTree> _interpolatableIdentifiers;
    private QueryContext _queryContext;

    public InterpolatedSyntaxVisitor(
        InterpolatorInvocationContext context,
        InterpolatedExpressionBuilder builder
    ) {
        _context = context;
        _builder = builder;

        _interpolatableIdentifiers = ImmutableDictionary<string, InterpolatedTree>.Empty
        .WithComparers(IdentifierEqualityComparer.Instance)
        .SetItems(
            // Register the identifiers of the parameters to the interpolated expression, less
            // the initial IInterpolationContext parameter
            from tup in GetLambdaParameters(context.InterpolatedExpression).Skip(1)
            select new KeyValuePair<string, InterpolatedTree>(
                tup.Parameter.Identifier.Text,
                _builder.CreateParameter(tup.ParameterType, tup.Parameter.Identifier.Text)
            )
        );

        _queryContext = QueryContext.Create(this);
    }

    private IEnumerable<(ParameterSyntax Parameter, ITypeSymbol ParameterType)> GetLambdaParameters(
        LambdaExpressionSyntax node
    ) {
        var lambdaType = (INamedTypeSymbol)_context.SemanticModel.GetTypeInfo(node).ConvertedType!;
        var delegateType = TypeSymbolHelpers.IsSubtype(lambdaType.ConstructUnboundGenericType(), _context.TypeSymbols.Expression1) switch {
            true => (INamedTypeSymbol)lambdaType.TypeArguments[0],
            false => lambdaType
        };

        var parameterTypes = delegateType.TypeArguments;

        switch(node) {
            case SimpleLambdaExpressionSyntax simple:
                yield return (simple.Parameter, parameterTypes[0]);
                break;

            case ParenthesizedLambdaExpressionSyntax parenthesized:
                var parameters = parenthesized.ParameterList.Parameters;
                foreach(var (parameter, parameterType) in parameters.Zip(parameterTypes.Take(parameters.Count)))
                    yield return (parameter, parameterType);

                break;

            default:
                throw new NotImplementedException();
        }
    }

    public override InterpolatedTree Visit(SyntaxNode? node) {
        return base.Visit(node)!;
    }

    public override InterpolatedTree DefaultVisit(SyntaxNode node) {
        return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
    }

    private InterpolatedTree VisitSplicingInvocation(InvocationExpressionSyntax node, IMethodSymbol method) {
        _context.SpliceCount += 1;

        return method.Name switch {
            "Splice" => VisitSplice(node, method),
            "SpliceBody" => VisitSpliceBody(node, method),
            "SpliceValue" => VisitSpliceValue(node, method),
            "SpliceQuoted" => VisitSpliceQuoted(node, method),
            _ => _context.Diagnostics.UnsupportedInterpolatedSyntax(node)
        };
    }

    private InterpolatedTree VisitSplice(InvocationExpressionSyntax node, IMethodSymbol method) {
        var resultType = method.TypeArguments[0];
        var evaluatedNode = node.ArgumentList.Arguments[0].Expression;

        if(_context.SemanticModel.GetTypeInfo(evaluatedNode).Type is not {} evaluatedType)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var identifier = _context.Builder.CreateIdentifier();

        return InterpolatedTree.Switch(
            InterpolatedTree.InstanceCall(
                _context.Builder.CreateTypeRef(evaluatedType),
                InterpolatedTree.Verbatim("Coerce"),
                [VisitEvaluatedSyntax(evaluatedNode)]
            ),
            [
                InterpolatedTree.SwitchCase(
                    InterpolatedTree.Concat(
                        InterpolatedTree.Verbatim($"var {identifier} when "),
                        _builder.CreateType(resultType),
                        InterpolatedTree.Verbatim($" == {identifier}.Type")
                    ),
                    InterpolatedTree.Verbatim(identifier)
                ),
                InterpolatedTree.SwitchCase(
                    InterpolatedTree.Verbatim($"var {identifier}"),
                    _builder.CreateExpression(nameof(Expression.Convert),
                        InterpolatedTree.Verbatim(identifier),
                        _builder.CreateType(resultType)
                    )
                )
            ]
        );
    }

    private InterpolatedTree VisitSpliceBody(InvocationExpressionSyntax node, IMethodSymbol method) {
        var identifer = _builder.CreateIdentifier();
        var parameterCount = method.Parameters.Length - 1;
        var resultType = method.Parameters[parameterCount].Type;
        var expressionNode = node.ArgumentList.Arguments[parameterCount];
        var expressionType = _context.SemanticModel.GetTypeInfo(expressionNode).Type;

        // Generate the interpolated parameter trees so that the nodes are interpolated in
        // declaration order.
        var parameterTrees = new List<InterpolatedTree>(parameterCount);
        for(var i = 0; i < parameterCount; i++)
            parameterTrees.Add(Visit(node.ArgumentList.Arguments[i].Expression));

        var expressionTree = VisitSpliceBodyExpression(expressionNode, method);

        // We'll use a switch expression with a single case to bind the evaluated expression tree
        return InterpolatedTree.Switch(expressionTree, [
            InterpolatedTree.SwitchCase(
                InterpolatedTree.Verbatim($"var {identifer}"),
                InterpolatedTree.StaticCall(
                    InterpolatedTree.Verbatim("global::Arborist.ExpressionHelper.Replace"),
                    [
                        InterpolatedTree.Verbatim($"{identifer}.Body"),
                        InterpolatedTree.StaticCall(
                            InterpolatedTree.Verbatim("global::Arborist.Internal.Collections.SmallDictionary.Create"),
                            [..(
                                from parameterIndex in Enumerable.Range(0, parameterCount)
                                select InterpolatedTree.StaticCall(
                                    InterpolatedTree.Verbatim($"new global::System.Collections.Generic.KeyValuePair<{_builder.ExpressionTypeName}, {_builder.ExpressionTypeName}>"),
                                    [
                                        InterpolatedTree.Verbatim($"{identifer}.Parameters[{parameterIndex}]"),
                                        parameterTrees[parameterIndex]
                                    ]
                                )
                            )]
                        )
                    ]
                )
            )
        ]);
    }

    private InterpolatedTree VisitSpliceBodyExpression(ArgumentSyntax node, IMethodSymbol method) {
        // If this is not a lambda literal, we can return the resulting lambda directly
        if(node.Expression is not LambdaExpressionSyntax)
            return VisitEvaluatedSyntax(node.Expression);

        // Otherwise we need to provide the target expression type for the lambda
        var expressionType = method.Parameters.Last().Type;

        return InterpolatedTree.InstanceCall(
            _builder.CreateTypeRef(expressionType),
            InterpolatedTree.Verbatim("Coerce"),
            [VisitEvaluatedSyntax(node.Expression)]
        );
    }

    private InterpolatedTree VisitSpliceValue(InvocationExpressionSyntax node, IMethodSymbol method) {
        var resultType = method.TypeArguments[0];
        var valueNode = node.ArgumentList.Arguments[0].Expression;

        return _builder.CreateExpression(nameof(Expression.Constant),
            VisitEvaluatedSyntax(valueNode),
            _builder.CreateType(method.Parameters[0].Type)
        );
    }

    private InterpolatedTree VisitSpliceQuoted(InvocationExpressionSyntax node, IMethodSymbol method) {
        var expressionNode = node.ArgumentList.Arguments[0].Expression;

        return _builder.CreateExpression(nameof(Expression.Quote),
            VisitEvaluatedSyntax(expressionNode)
        );
    }

    private InterpolatedTree VisitEvaluatedSyntax(SyntaxNode node) =>
        new EvaluatedSyntaxVisitor(_context, _builder, _interpolatableIdentifiers)
        .Visit(node);

    private bool TryGetSpliceMethod(InvocationExpressionSyntax node, out IMethodSymbol spliceMethod) {
        spliceMethod = default!;
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
            return false;

        if(!TypeSymbolHelpers.IsSubtype(methodSymbol.ContainingType, _context.TypeSymbols.IInterpolationContext))
            return false;

        spliceMethod = methodSymbol;
        return true;
    }

    public override InterpolatedTree VisitIdentifierName(IdentifierNameSyntax node) {
        var symbol = _context.SemanticModel.GetSymbolInfo(node).Symbol;
        if(symbol is not null && !TypeSymbolHelpers.IsAccessible(symbol))
            return _context.Diagnostics.InaccesibleSymbol(symbol, node);

        if(!_interpolatableIdentifiers.TryGetValue(node.Identifier.Text, out var identifierTree))
            return _context.Diagnostics.Closure(node);

        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} type)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        return identifierTree;
    }

    public override InterpolatedTree VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not IArrayTypeSymbol arrayType)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        return _builder.CreateExpression(nameof(Expression.NewArrayInit), [
            _builder.CreateType(arrayType.ElementType),
            ..node.Initializer.Expressions.Select(Visit)
        ]);
    }

    public override InterpolatedTree VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(TryGetSpliceMethod(node, out var spliceMethod)) {
            return VisitSplicingInvocation(node, spliceMethod);
        } else {
            return VisitInvocation(node);
        }
    }

    private InterpolatedTree VisitInvocation(InvocationExpressionSyntax node) {
        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IMethodSymbol { ReducedFrom: {} } method:
                return _builder.CreateExpression(nameof(Expression.Call),
                    _builder.CreateMethodInfo(method, node),
                    _builder.CreateExpressionArray([
                        Visit(node.Expression),
                        ..node.ArgumentList.Arguments.Select(a => Visit(a.Expression))
                    ])
                );

            case IMethodSymbol { IsStatic: true } method:
                return _builder.CreateExpression(nameof(Expression.Call),
                    _builder.CreateMethodInfo(method, node),
                    _builder.CreateExpressionArray(node.ArgumentList.Arguments.Select(a => Visit(a.Expression)))
                );

            case IMethodSymbol method:
                return _builder.CreateExpression(nameof(Expression.Call),
                    Visit(node.Expression),
                    _builder.CreateMethodInfo(method, node),
                    _builder.CreateExpressionArray(node.ArgumentList.Arguments.Select(a => Visit(a.Expression)))
                );

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
    }

    public override InterpolatedTree VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        var symbol = _context.SemanticModel.GetSymbolInfo(node).Symbol;
        if(_context.IsInterpolationDataAccess(symbol))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        return VisitMemberAccess(node, symbol);
    }

    private InterpolatedTree VisitMemberAccess(MemberAccessExpressionSyntax node, ISymbol? symbol) {
        switch(symbol) {
            case IFieldSymbol field:
                return _builder.CreateExpression(nameof(Expression.Field),
                    (field.IsStatic || field.IsConst) switch {
                        true => _builder.CreateDefaultValue(_context.TypeSymbols.Expression.WithNullableAnnotation(NullableAnnotation.Annotated)),
                        false => Visit(node.Expression)
                    },
                    InterpolatedTree.Concat(
                        InterpolatedTree.InstanceCall(
                            _builder.CreateType(field.ContainingType),
                            InterpolatedTree.Verbatim(nameof(Type.GetField)),
                            [InterpolatedTree.Verbatim($"\"{field.Name}\"")]
                        ),
                        InterpolatedTree.Verbatim("!")
                    )
                );

            case IPropertySymbol property:
                return _builder.CreateExpression(nameof(Expression.Property),
                    property.IsStatic switch {
                        true => _builder.CreateDefaultValue(_context.TypeSymbols.Expression.WithNullableAnnotation(NullableAnnotation.Annotated)),
                        false => Visit(node.Expression)
                    },
                    InterpolatedTree.Concat(
                        InterpolatedTree.InstanceCall(
                            _builder.CreateType(property.ContainingType),
                            InterpolatedTree.Verbatim(nameof(Type.GetProperty)),
                            [InterpolatedTree.Verbatim($"\"{property.Name}\"")]
                        ),
                        InterpolatedTree.Verbatim("!")
                    )
                );

            case IMethodSymbol method:
                return Visit(node.Expression);

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        }
    }

    public override InterpolatedTree VisitDefaultExpression(DefaultExpressionSyntax node) {
        var typeInfo = _context.SemanticModel.GetTypeInfo(node);
        return _builder.CreateExpression(nameof(Expression.Default), _builder.CreateType(typeInfo.Type!));
    }

    public override InterpolatedTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        return _builder.CreateAnonymousClassExpression(
            typeSymbol,
            [..node.Initializers.Select(i => Visit(i.Expression))]
        );
    }

    public override InterpolatedTree VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    public override InterpolatedTree VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    private InterpolatedTree VisitBaseObjectCreationExpression(BaseObjectCreationExpressionSyntax node) {
        var methodSymbol = (IMethodSymbol)_context.SemanticModel.GetSymbolInfo(node).Symbol!;

        var constructorInfo = InterpolatedTree.Concat(
            InterpolatedTree.InstanceCall(
                _builder.CreateType(methodSymbol.ContainingType),
                InterpolatedTree.Verbatim(nameof(Type.GetConstructor)),
                [_builder.CreateTypeArray(methodSymbol.Parameters.Select(p => p.Type))]
            ),
            InterpolatedTree.Verbatim("!")
        );

        var newExpr = _builder.CreateExpression(nameof(Expression.New),
            constructorInfo,
            _builder.CreateExpressionArray(node.ArgumentList switch {
                null => Array.Empty<InterpolatedTree>(),
                not null => node.ArgumentList.Arguments.Select(a => Visit(a.Expression))
            })
        );

        if(node.Initializer is null)
            return newExpr;

        return node.Initializer.Kind() switch {
            SyntaxKind.ObjectInitializerExpression =>
                _builder.CreateExpression(nameof(Expression.MemberInit), newExpr, Visit(node.Initializer)),
            SyntaxKind.CollectionInitializerExpression =>
                _builder.CreateExpression(nameof(Expression.ListInit), newExpr, Visit(node.Initializer)),
            _ => _context.Diagnostics.UnsupportedInterpolatedSyntax(node.Initializer)
        };
    }

    public override InterpolatedTree VisitInitializerExpression(InitializerExpressionSyntax node) =>
        // This is a pain in the butt because from a syntactical standpoint an initializer is a "bracketed list
        // of things" (e.g. a collection initializer is a bracketed list of bracketed lists), but we care very
        // much about the context in which the bracketed list and its elements occur.
        node.Kind() switch {
            SyntaxKind.ObjectInitializerExpression => InterpolatedTree.Concat(
                InterpolatedTree.Verbatim("new global::System.Linq.Expressions.MemberBinding[] "),
                InterpolatedTree.Initializer([..node.Expressions.Select(VisitObjectInitializerElement)])
            ),
            SyntaxKind.CollectionInitializerExpression => InterpolatedTree.Concat(
                InterpolatedTree.Verbatim("new global::System.Linq.Expressions.ElementInit[] "),
                InterpolatedTree.Initializer([..node.Expressions.Select(VisitCollectionInitializerElement)])
            ),
            _ => _context.Diagnostics.UnsupportedInterpolatedSyntax(node)
        };

    private InterpolatedTree VisitObjectInitializerElement(ExpressionSyntax node) {
        switch(node) {
            case AssignmentExpressionSyntax { Left: IdentifierNameSyntax identifier } assignment:
                var identifierSymbol = _context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                if(identifierSymbol is null)
                    return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
                if(!TypeSymbolHelpers.IsAccessible(identifierSymbol))
                    return _context.Diagnostics.InaccesibleSymbol(identifierSymbol, identifier);

                return _builder.CreateExpression(nameof(Expression.Bind),
                    InterpolatedTree.Concat(
                        InterpolatedTree.InstanceCall(
                            _builder.CreateType(identifierSymbol.ContainingType),
                            InterpolatedTree.Verbatim(nameof(Type.GetMember)),
                            [InterpolatedTree.Verbatim($"\"{identifier.Identifier.Text}\"")]
                        ),
                        InterpolatedTree.Verbatim("!")
                    ),
                    Visit(assignment.Right)
                );

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
        if(node.Parent?.Parent is not {} parentSymbol)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        if(_context.SemanticModel.GetTypeInfo(parentSymbol).Type is not {} parentType)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        // TODO: we should probably look into overload resolution, but this is good enough for the
        // vast majority of cases.
        var argumentTypes = argumentExpressions.Select(e => _context.SemanticModel.GetTypeInfo(e)).ToList();
        var methodSymbol = parentType.GetMembers("Add").OfType<IMethodSymbol>().FirstOrDefault(
            m => !m.IsStatic
            && m.Parameters.Length == argumentExpressions.Count
            && m.Parameters.All(p => TypeSymbolHelpers.IsSubtype(argumentTypes[p.Ordinal].Type, p.Type))
        );

        if(methodSymbol is null)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(methodSymbol))
            return _context.Diagnostics.InaccesibleSymbol(methodSymbol, node);

        return _builder.CreateExpression(nameof(Expression.ElementInit), [
            _builder.CreateMethodInfo(methodSymbol, default),
            _builder.CreateExpressionArray([..argumentExpressions.Select(Visit)])
        ]);
    }

    public override InterpolatedTree VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) =>
        Visit(node.Expression)!;

    public override InterpolatedTree VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
        var snapshot = _interpolatableIdentifiers;
        try {
            var parameterList = new List<InterpolatedTree>();
            foreach(var (parameter, parameterType) in GetLambdaParameters(node)) {
                var parameterTree = _builder.CreateParameter(parameterType, parameter.Identifier.Text);
                parameterList.Add(parameterTree);
                _interpolatableIdentifiers = _interpolatableIdentifiers.SetItem(parameter.Identifier.Text, parameterTree);
            }

            return _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.Body),
                ..parameterList
            ]);
        } finally {
            _interpolatableIdentifiers = snapshot;
        }
    }

    public override InterpolatedTree VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        // Add any newly defined parameters to the set of interpolatable parameters
        var snapshot = _interpolatableIdentifiers;
        try {
            var parameterList = new List<InterpolatedTree>(node.ParameterList.Parameters.Count);
            foreach(var (parameter, parameterType) in GetLambdaParameters(node)) {
                var parameterTree = _builder.CreateParameter(parameterType, parameter.Identifier.Text);
                parameterList.Add(parameterTree);
                _interpolatableIdentifiers = _interpolatableIdentifiers.SetItem(parameter.Identifier.Text, parameterTree);
            }

            return _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.Body),
                ..parameterList
            ]);
        } finally {
            _interpolatableIdentifiers = snapshot;
        }
    }

    public override InterpolatedTree VisitConditionalExpression(ConditionalExpressionSyntax node) {
        return _builder.CreateExpression(nameof(Expression.Condition),
            Visit(node.Condition),
            Visit(node.WhenTrue),
            Visit(node.WhenFalse),
            _builder.CreateType(_context.SemanticModel.GetTypeInfo(node).Type!)
        );
    }

    public override InterpolatedTree VisitBinaryExpression(BinaryExpressionSyntax node) =>
        _builder.CreateExpression(nameof(Expression.MakeBinary),
            _builder.CreateExpressionType(node),
            Visit(node.Left),
            Visit(node.Right)
        );

    public override InterpolatedTree VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) =>
        _builder.CreateExpression(nameof(Expression.MakeUnary),
            _builder.CreateExpressionType(node),
            Visit(node.Operand)
        );

    public override InterpolatedTree VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) =>
        _builder.CreateExpression(nameof(Expression.MakeUnary),
            _builder.CreateExpressionType(node),
            Visit(node.Operand)
        );

    public override InterpolatedTree VisitLiteralExpression(LiteralExpressionSyntax node) {
        var type = _context.SemanticModel.GetTypeInfo(node).Type!;

        return _builder.CreateExpression(nameof(Expression.Constant),
            InterpolatedTree.Verbatim(node.ToString().Trim()),
            _builder.CreateType(type)
        );
    }
}
