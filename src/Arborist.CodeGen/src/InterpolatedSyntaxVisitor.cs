using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

internal class InterpolatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedExpressionTree> {
    private readonly InterpolatorInvocationContext _context;
    private readonly InterpolatedExpressionBuilder _builder;
    private ImmutableHashSet<string> _interpolatableParameters;

    public InterpolatedSyntaxVisitor(
        InterpolatorInvocationContext context,
        InterpolatedExpressionBuilder builder
    ) {
        _context = context;
        _builder = builder;

        _interpolatableParameters = ImmutableHashSet.CreateRange(
            IdentifierEqualityComparer.Instance,
            from parameter in context.InterpolatedExpressionParameters
            select parameter.Identifier.Text
        );
    }

    public override InterpolatedExpressionTree Visit(SyntaxNode? node) {
        return base.Visit(node)!;
    }

    public override InterpolatedExpressionTree DefaultVisit(SyntaxNode node) {
        return _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported);
    }

    private InterpolatedExpressionTree VisitSplicingInvocation(InvocationExpressionSyntax node, IMethodSymbol method) {
        _context.SpliceCount += 1;

        return method.Name switch {
            "Splice" => VisitSplice(node, method),
            "SpliceBody" => VisitSpliceBody(node, method),
            "SpliceValue" => VisitSpliceValue(node, method),
            "SpliceQuoted" => VisitSpliceQuoted(node, method),
            _ => _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported)
        };
    }

    private InterpolatedExpressionTree VisitSplice(InvocationExpressionSyntax node, IMethodSymbol method) {
        var resultType = method.TypeArguments[0];
        var evaluatedNode = node.ArgumentList.Arguments[0].Expression;
        var evaluatedType = _context.SemanticModel.GetTypeInfo(evaluatedNode).Type;

        var evaluated = VisitEvaluatedSyntax(evaluatedNode);
        if(evaluatedType is null)
            return evaluated;
        if(SymbolEqualityComparer.IncludeNullability.Equals(resultType, evaluatedType))
            return evaluated;

        return _builder.CreateExpression(nameof(Expression.Convert),
            evaluated,
            _builder.CreateType(resultType)
        );
    }

    private InterpolatedExpressionTree VisitSpliceBody(InvocationExpressionSyntax node, IMethodSymbol method) {
        var identifer = _builder.CreateIdentifier();
        var parameterCount = method.Parameters.Length - 1;
        var resultType = method.Parameters[parameterCount].Type;
        var expressionNode = node.ArgumentList.Arguments[parameterCount];
        var expressionType = _context.SemanticModel.GetTypeInfo(expressionNode).Type;

        // Generate the interpolated parameter trees so that the nodes are interpolated in
        // declaration order.
        var parameterTrees = new List<InterpolatedExpressionTree>(parameterCount);
        for(var i = 0; i < parameterCount; i++)
            parameterTrees.Add(Visit(node.ArgumentList.Arguments[i].Expression));

        var expressionTree = VisitSpliceBodyExpression(expressionNode, method);

        // We'll use a switch expression with a single case to bind the evaluated expression tree
        return InterpolatedExpressionTree.Switch(expressionTree, [
            InterpolatedExpressionTree.SwitchCase(
                InterpolatedExpressionTree.Verbatim($"var {identifer}"),
                InterpolatedExpressionTree.StaticCall(
                    "global::Arborist.ExpressionHelper.Replace", [
                        InterpolatedExpressionTree.Verbatim($"{identifer}.Body"),
                        InterpolatedExpressionTree.StaticCall(
                            "global::Arborist.Internal.Collections.SmallDictionary.Create", [..(
                                from parameterIndex in Enumerable.Range(0, parameterCount)
                                select InterpolatedExpressionTree.StaticCall(
                                    $"new global::System.Collections.Generic.KeyValuePair<{_builder.ExpressionTypeName}, {_builder.ExpressionTypeName}>", [
                                        InterpolatedExpressionTree.Verbatim($"{identifer}.Parameters[{parameterIndex}]"),
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

    private InterpolatedExpressionTree VisitSpliceBodyExpression(ArgumentSyntax node, IMethodSymbol method) {
        // If this is not a lambda literal, we can return the resulting lambda directly
        if(node.Expression is not LambdaExpressionSyntax)
            return VisitEvaluatedSyntax(node.Expression);

        // Otherwise we need to provide the target expression type for the lambda
        var expressionType = method.Parameters.Last().Type;

        return InterpolatedExpressionTree.InstanceCall(
            _builder.CreateTypeRef(expressionType),
            "Coerce",
            [VisitEvaluatedSyntax(node.Expression)]
        );
    }

    private InterpolatedExpressionTree VisitSpliceValue(InvocationExpressionSyntax node, IMethodSymbol method) {
        var resultType = method.TypeArguments[0];
        var valueNode = node.ArgumentList.Arguments[0].Expression;

        return _builder.CreateExpression(nameof(Expression.Constant),
            VisitEvaluatedSyntax(valueNode),
            _builder.CreateType(method.Parameters[0].Type)
        );
    }

    private InterpolatedExpressionTree VisitSpliceQuoted(InvocationExpressionSyntax node, IMethodSymbol method) {
        var expressionNode = node.ArgumentList.Arguments[0].Expression;

        return _builder.CreateExpression(nameof(Expression.Quote),
            VisitEvaluatedSyntax(expressionNode)
        );
    }

    private InterpolatedExpressionTree VisitEvaluatedSyntax(SyntaxNode node) =>
        new EvaluatedSyntaxVisitor(_context, _builder, _interpolatableParameters).Visit(node);

    private bool TryGetSpliceMethod(InvocationExpressionSyntax node, out IMethodSymbol spliceMethod) {
        spliceMethod = default!;
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
            return false;

        if(!TypeSymbolHelpers.IsSubtype(methodSymbol.ContainingType, _context.TypeSymbols.IInterpolationContext))
            return false;

        spliceMethod = methodSymbol;
        return true;
    }

    public override InterpolatedExpressionTree VisitIdentifierName(IdentifierNameSyntax node) {
        if(!_interpolatableParameters.Contains(node.Identifier.Text))
            return _context.Diagnostics.Closure(node, InterpolatedExpressionTree.Unsupported);

        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} type)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported);

        return _builder.CreateParameter(type, node.Identifier.Text);
    }

    public override InterpolatedExpressionTree? VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not IArrayTypeSymbol arrayType)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported);

        return _builder.CreateExpression(nameof(Expression.NewArrayInit), [
            _builder.CreateType(arrayType.ElementType),
            ..node.Initializer.Expressions.Select(Visit)
        ]);
    }

    public override InterpolatedExpressionTree VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(TryGetSpliceMethod(node, out var spliceMethod)) {
            return VisitSplicingInvocation(node, spliceMethod);
        } else {
            return VisitInvocation(node);
        }
    }


    private InterpolatedExpressionTree VisitInvocation(InvocationExpressionSyntax node) {
        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IMethodSymbol { IsExtensionMethod: true, IsGenericMethod: true } method:
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
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        }
    }

    public override InterpolatedExpressionTree VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        var symbol = _context.SemanticModel.GetSymbolInfo(node).Symbol;
        if(_context.IsInterpolationDataAccess(symbol))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported);

        return VisitMemberAccess(node, symbol);
    }

    private InterpolatedExpressionTree VisitMemberAccess(MemberAccessExpressionSyntax node, ISymbol? symbol) {
        switch(symbol) {
            case IFieldSymbol field:
                return _builder.CreateExpression(nameof(Expression.Field),
                    (field.IsStatic || field.IsConst) switch {
                        true => _builder.CreateDefaultValue(_context.TypeSymbols.Expression.WithNullableAnnotation(NullableAnnotation.Annotated)),
                        false => Visit(node.Expression)
                    },
                    InterpolatedExpressionTree.Concat(
                        InterpolatedExpressionTree.InstanceCall(
                            _builder.CreateType(field.ContainingType),
                            nameof(Type.GetField),
                            [InterpolatedExpressionTree.Verbatim($"\"{field.Name}\"")]
                        ),
                        InterpolatedExpressionTree.Verbatim("!")
                    )
                );

            case IPropertySymbol property:
                return _builder.CreateExpression(nameof(Expression.Property),
                    property.IsStatic switch {
                        true => _builder.CreateDefaultValue(_context.TypeSymbols.Expression.WithNullableAnnotation(NullableAnnotation.Annotated)),
                        false => Visit(node.Expression)
                    },
                    InterpolatedExpressionTree.Concat(
                        InterpolatedExpressionTree.InstanceCall(
                            _builder.CreateType(property.ContainingType),
                            nameof(Type.GetProperty),
                            [InterpolatedExpressionTree.Verbatim($"\"{property.Name}\"")]
                        ),
                        InterpolatedExpressionTree.Verbatim("!")
                    )
                );

            case IMethodSymbol method:
                return Visit(node.Expression);

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        }
    }

    public override InterpolatedExpressionTree VisitDefaultExpression(DefaultExpressionSyntax node) {
        var typeInfo = _context.SemanticModel.GetTypeInfo(node);
        return _builder.CreateExpression(nameof(Expression.Default), _builder.CreateType(typeInfo.Type!));
    }

    public override InterpolatedExpressionTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) {
        // An anonymous type has a single constructor accepting each of its properties as arguments
        var typeSymbol = (ITypeSymbol)_context.SemanticModel.GetTypeInfo(node).Type!;
        return _builder.CreateExpression(nameof(Expression.New), [
            InterpolatedExpressionTree.Indexer(
                InterpolatedExpressionTree.InstanceCall(
                    _builder.CreateType(typeSymbol),
                    nameof(Type.GetConstructors),
                    []
                ),
                InterpolatedExpressionTree.Verbatim("0")
            ),
            ..node.Initializers.Select(i => Visit(i.Expression))
        ]);
    }

    public override InterpolatedExpressionTree VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    public override InterpolatedExpressionTree VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    private InterpolatedExpressionTree VisitBaseObjectCreationExpression(BaseObjectCreationExpressionSyntax node) {
        var methodSymbol = (IMethodSymbol)_context.SemanticModel.GetSymbolInfo(node).Symbol!;

        var constructorInfo = InterpolatedExpressionTree.InstanceCall(
            _builder.CreateType(methodSymbol.ContainingType),
            nameof(Type.GetConstructor),
            [_builder.CreateTypeArray(methodSymbol.Parameters.Select(p => p.Type))]
        );

        var newExpr = _builder.CreateExpression(nameof(Expression.New),
            constructorInfo,
            _builder.CreateExpressionArray(node.ArgumentList switch {
                null => Array.Empty<InterpolatedExpressionTree>(),
                not null => node.ArgumentList.Arguments.Select(a => Visit(a.Expression))
            })
        );

        if(node.Initializer is null)
            return newExpr;

        return _builder.CreateExpression(nameof(Expression.MemberInit),
            newExpr,
            Visit(node.Initializer)
        );
    }
    
    public override InterpolatedExpressionTree VisitInitializerExpression(InitializerExpressionSyntax node) {
        var typeSymbol = (ITypeSymbol)_context.SemanticModel.GetSymbolInfo(node).Symbol!;

        switch(node.Kind()) {
            case SyntaxKind.ObjectInitializerExpression:
                return InterpolatedExpressionTree.ObjectInit(
                    InterpolatedExpressionTree.Verbatim("new global::System.Linq.Expressions.MemberBinding[]"),
                    node.Expressions.Select(ie => VisitObjectInitializerExpressionSyntax(typeSymbol, ie)).ToList()
                );

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        }
    }

    private InterpolatedExpressionTree VisitObjectInitializerExpressionSyntax(ITypeSymbol objectType, ExpressionSyntax node) {
        switch(node) {
            case AssignmentExpressionSyntax { Left: IdentifierNameSyntax identifier } assignment:
                return _builder.CreateExpression(nameof(Expression.Bind),
                    InterpolatedExpressionTree.InstanceCall(
                        _builder.CreateType(objectType),
                        nameof(Type.GetMember),
                        [InterpolatedExpressionTree.Verbatim($"\"{identifier.Identifier}\"")]
                    ),
                    Visit(assignment.Right)
                );

            default:
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        }
    }
    
    public override InterpolatedExpressionTree VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) =>
        Visit(node.Expression)!;

    public override InterpolatedExpressionTree VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
        var snapshot = _interpolatableParameters;
        _interpolatableParameters = _interpolatableParameters.Add(node.Parameter.Identifier.Text);
        try {
            var lambdaType = (INamedTypeSymbol)_context.SemanticModel.GetTypeInfo(node).ConvertedType!;
            var parameterType = lambdaType.TypeArguments[0].OriginalDefinition;
            var parameterName = node.Parameter.Identifier.ValueText;
            var parameter = _builder.CreateParameter(parameterType, parameterName);

            return _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.Body),
                parameter
            ]);
        } finally {
            _interpolatableParameters = snapshot;
        }
    }

    public override InterpolatedExpressionTree VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        // Add any newly defined parameters to the set of interpolatable parameters
        var snapshot = _interpolatableParameters;
        _interpolatableParameters = _interpolatableParameters.Union(node.ParameterList.Parameters.Select(p => p.Identifier.Text));
        try {
            var lambdaType = (INamedTypeSymbol)_context.SemanticModel.GetTypeInfo(node).ConvertedType!;
            var parameterList = new List<InterpolatedExpressionTree>(node.ParameterList.Parameters.Count);
            for(var i = 0; i < node.ParameterList.Parameters.Count; i++) {
                var parameterType = lambdaType.TypeArguments[i].OriginalDefinition;
                var parameterName = node.ParameterList.Parameters[i].Identifier.ValueText;
                parameterList.Add(_builder.CreateParameter(parameterType, parameterName));
            }

            return _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.Body),
                ..parameterList
            ]);
        } finally {
            _interpolatableParameters = snapshot;
        }
    }

    public override InterpolatedExpressionTree VisitConditionalExpression(ConditionalExpressionSyntax node) {
        return _builder.CreateExpression(nameof(Expression.Condition),
            Visit(node.Condition),
            Visit(node.WhenTrue),
            Visit(node.WhenFalse),
            _builder.CreateType(_context.SemanticModel.GetTypeInfo(node).Type!)
        );
    }

    public override InterpolatedExpressionTree VisitBinaryExpression(BinaryExpressionSyntax node) =>
        _builder.CreateExpression(nameof(Expression.MakeBinary),
            _builder.CreateExpressionType(node),
            Visit(node.Left),
            Visit(node.Right)
        );

    public override InterpolatedExpressionTree VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) =>
        _builder.CreateExpression(nameof(Expression.MakeUnary),
            _builder.CreateExpressionType(node),
            Visit(node.Operand)
        );

    public override InterpolatedExpressionTree VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) =>
        _builder.CreateExpression(nameof(Expression.MakeUnary),
            _builder.CreateExpressionType(node),
            Visit(node.Operand)
        );

    public override InterpolatedExpressionTree VisitLiteralExpression(LiteralExpressionSyntax node) {
        var type = _context.SemanticModel.GetTypeInfo(node).Type!;

        return _builder.CreateExpression(nameof(Expression.Constant),
            InterpolatedExpressionTree.Verbatim(node.ToFullString()),
            _builder.CreateType(type)
        );
    }
}
