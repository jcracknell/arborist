using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Arborist.CodeGen;

internal class InterpolatorExpressionSyntaxVisitor : CSharpSyntaxVisitor<string> {
    private readonly InterpolatorInvocationContext _context;
    private readonly ImmutableHashSet<SyntaxToken> _forbiddenParameters;
    private ArgumentSyntax? _evaluatedArgument;
    private readonly CodeFactory _codeFactory;

    public InterpolatorExpressionSyntaxVisitor(
        LambdaExpressionSyntax lambdaExpression,
        InterpolatorInvocationContext context
    ) {
        _context = context;
        _codeFactory = new CodeFactory(context);
        _forbiddenParameters = lambdaExpression switch {
            SimpleLambdaExpressionSyntax simple =>
                ImmutableHashSet.Create(simple.Parameter.Identifier),
            ParenthesizedLambdaExpressionSyntax parenthesized =>
                ImmutableHashSet.CreateRange(parenthesized.ParameterList.Parameters.Select(p => p.Identifier)),
            _ => throw new NotImplementedException()
        };
    }

    private bool TryGetSpliceMethod(InvocationExpressionSyntax node, out IMethodSymbol spliceMethod) {
        spliceMethod = default!;
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
            return false;

        if(!TypeSymbolHelpers.IsSubtype(methodSymbol.ContainingType, _context.TypeSymbols.IInterpolationContext))
            return false;

        spliceMethod = methodSymbol;
        return true;
    }

    private bool IsInterpolationDataAccess(ISymbol? symbol) =>
        symbol is IPropertySymbol { Name: "Data", ContainingType: { IsGenericType: true } } property
        && SymbolEqualityComparer.Default.Equals(
            property.ContainingType.ConstructUnboundGenericType(),
            _context.TypeSymbols.IInterpolationContext1
        );

    public override string DefaultVisit(SyntaxNode node) {
        return _context.Diagnostic(
            "???",
            code: DiagnosticCodes.ARB001_UnsupportedSyntax,
            title: "Unsupported Syntax",
            message: $"Syntax node {node} is not currently supported by compile-time interpolation.",
            syntax: node
        );
    }

    public override string VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(TryGetSpliceMethod(node, out var spliceMethod)) {
            return VisitSplice(node, spliceMethod);
        } else {
            return VisitInvocation(node);
        }
    }

    private string VisitSplice(InvocationExpressionSyntax node, IMethodSymbol spliceMethod) {
        foreach(var parameter in spliceMethod.Parameters) {
            if(parameter.Ordinal == -1)
                continue;

            var attributes = parameter.GetAttributes();
            var argument = node.ArgumentList.Arguments[parameter.Ordinal];

            bool HasAttribute(ITypeSymbol attr) =>
                attributes.Any(a => TypeSymbolHelpers.IsSubtype(a.AttributeClass, attr));

            if(HasAttribute(_context.TypeSymbols.InterpolatedSpliceParameterAttribute)) {

            } else if(HasAttribute(_context.TypeSymbols.EvaluatedSpliceParameterAttribute)) {
                _evaluatedArgument = argument;
                try {
                    argument.Accept(this);
                } finally {
                    _evaluatedArgument = null;
                }
            } else {
                argument.Accept(this);
            }
        }

        throw new NotImplementedException();
    }

    private string VisitInvocation(InvocationExpressionSyntax node) {
        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IMethodSymbol { IsExtensionMethod: true } method:
                return _codeFactory.CreateExpression(nameof(Expression.Call),
                    _codeFactory.CreateMethodInfo(method),
                    _codeFactory.CreateExpressionArray(
                        new object?[] { Visit(node.Expression) }
                        .Concat(node.ArgumentList.Arguments.Select(a => Visit(a.Expression)))
                    )
                );

            case IMethodSymbol { IsStatic: true } method:
                return _codeFactory.CreateExpression(nameof(Expression.Call),
                    _codeFactory.CreateMethodInfo(method),
                    _codeFactory.CreateExpressionArray(node.ArgumentList.Arguments.Select(a => Visit(a.Expression)))
                );

            case IMethodSymbol method:
                return _codeFactory.CreateExpression(nameof(Expression.Call),
                    Visit(node.Expression),
                    _codeFactory.CreateMethodInfo(method),
                    _codeFactory.CreateExpressionArray(node.ArgumentList.Arguments.Select(a => Visit(a.Expression)))
                );

            default:
                throw new NotImplementedException();
        }
    }

    public override string? VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        var symbol = _context.SemanticModel.GetSymbolInfo(node).Symbol;
        if(IsInterpolationDataAccess(symbol))
            throw new NotImplementedException();

        return VisitMemberAccess(node, symbol);
    }

    private string? VisitMemberAccess(MemberAccessExpressionSyntax node, ISymbol? symbol) {
        switch(symbol) {
            case IFieldSymbol field:
                return _codeFactory.CreateExpression(nameof(Expression.Field),
                    Visit(node.Expression),
                    $"{_codeFactory.CreateType(field.Type)}.{nameof(Type.GetProperty)}(\"{field.Name}\")"
                );
            case IPropertySymbol property:
                return _codeFactory.CreateExpression(nameof(Expression.Property),
                    Visit(node.Expression),
                    $"{_codeFactory.CreateType(property.Type)}.{nameof(Type.GetProperty)}(\"{property.Name}\")"
                );
            default:
                throw new NotImplementedException();
        }
    }

    public override string? VisitDefaultExpression(DefaultExpressionSyntax node) {
        var typeInfo = _context.SemanticModel.GetTypeInfo(node);
        return _codeFactory.CreateExpression(nameof(Expression.Default), _codeFactory.CreateType(typeInfo.Type!));
    }

    public override string? VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) {
        // An anonymous type has a single constructor accepting each of its properties as arguments
        var typeSymbol = (ITypeSymbol)_context.SemanticModel.GetSymbolInfo(node).Symbol!;
        return _codeFactory.CreateExpression(nameof(Expression.New),
            new[] { $"{_codeFactory.CreateType(typeSymbol)}.GetConstructors()[0]" }
            .Concat(node.Initializers.Select(i => Visit(i.Expression)))
        );
    }

    public override string? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) {
        var symbolInfo = _context.SemanticModel.GetSymbolInfo(node);
        var methodSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Single();

        var constructorInfo = string.Concat(
            _codeFactory.CreateType(methodSymbol.ContainingType),
            $".GetConstructor({_codeFactory.CreateTypeArray(methodSymbol.Parameters.Select(p => p.Type))})"
        );

        var newExpr = _codeFactory.CreateExpression(nameof(Expression.New),
            constructorInfo,
            _codeFactory.CreateExpressionArray(node.ArgumentList?.Arguments.Select(Visit) ?? Array.Empty<string>())
        );

        if(node.Initializer is null)
            return newExpr;

        return _codeFactory.CreateExpression(nameof(Expression.MemberInit),
            newExpr,
            Visit(node.Initializer)
        );
    }

    public override string? VisitInitializerExpression(InitializerExpressionSyntax node) {
        var typeSymbol = (ITypeSymbol)_context.SemanticModel.GetSymbolInfo(node).Symbol!;

        switch(node.Kind()) {
            case SyntaxKind.ObjectInitializerExpression:
                return node.Expressions.MkString(
                    "new global::System.Linq.Expressions.MemberBinding[] { ",
                    ie => VisitObjectInitializerExpressionSyntax(typeSymbol, ie),
                    ", ",
                    "}"
                );

            default:
                throw new NotImplementedException();
        }
    }

    private string? VisitObjectInitializerExpressionSyntax(ITypeSymbol objectType, ExpressionSyntax node) {
        switch(node) {
            case AssignmentExpressionSyntax { Left: IdentifierNameSyntax identifier } assignment:
                return _codeFactory.CreateExpression(nameof(Expression.Bind),
                    $"{_codeFactory.CreateType(objectType)}.GetMember(\"{identifier.Identifier}\")",
                    Visit(assignment.Right)
                );

            default:
                throw new NotImplementedException();
        }
    }

    public override string VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) =>
        Visit(node.Expression)!;

    public override string VisitBinaryExpression(BinaryExpressionSyntax node) =>
        _codeFactory.CreateExpression(nameof(Expression.MakeBinary),
            _codeFactory.CreateExpressionType(node),
            Visit(node.Left),
            Visit(node.Right)
        );

    public override string VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) =>
        _codeFactory.CreateExpression(nameof(Expression.MakeUnary),
            _codeFactory.CreateExpressionType(node),
            Visit(node.Operand)
        );

    public override string VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) =>
        _codeFactory.CreateExpression(nameof(Expression.MakeUnary),
            _codeFactory.CreateExpressionType(node),
            Visit(node.Operand)
        );

    public override string? VisitLiteralExpression(LiteralExpressionSyntax node) =>
        _codeFactory.CreateExpression(nameof(Expression.Constant), node.ToFullString());
}
