using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq.Expressions;

namespace Arborist.CodeGen;

internal class CodeFactory {
    private const string Unsupported = "???";

    private readonly InterpolatorInvocationContext _context;
    private readonly Dictionary<(LocalDefinitionType, ISymbol), LocalDefinition> _localRegistrations = new();

    public CodeFactory(InterpolatorInvocationContext context) {
        _context = context;
    }

    public IEnumerable<LocalDefinition> LocalRegistrations =>
        _localRegistrations.Values.OrderBy(r => r.Order);

    private string UnsupportedSyntax(SyntaxNode node) =>
        _context.Diagnostic(
            result: Unsupported,
            code: DiagnosticCodes.ARB001_UnsupportedSyntax,
            title: "Unsupported Syntax",
            message: $"Syntax {node} is unsupported.",
            syntax: node
        );

    private string UnsupportedSymbol(ISymbol symbol) =>
        throw new NotImplementedException();

    public string CreateExpression(string factoryName, params object?[] args) =>
        CreateExpression(factoryName, args.AsEnumerable());

    public string CreateExpression(string factoryName, IEnumerable<object?> args) =>
        args.MkString(
            $"global::System.Linq.Expressions.Expression.{factoryName}(\n",
            a => a?.ToString(),
            ",\n",
            "\n)"
        );

    public string CreateExpressionArray(IEnumerable<object?> elements) =>
        elements.MkString("new global::System.Linq.Expressions.Expression[] {", ", ", "}");

    public string CreateExpressionType(SyntaxNode syntax) =>
        $"global::System.Linq.Expressions.ExpressionType.{CreateExpressionTypeName(syntax)}";

    private string CreateExpressionTypeName(SyntaxNode syntax) => syntax.Kind() switch {
        // Logic
        SyntaxKind.LogicalNotExpression => nameof(ExpressionType.Not),
        SyntaxKind.LogicalAndExpression => nameof(ExpressionType.AndAlso),
        SyntaxKind.LogicalOrExpression => nameof(ExpressionType.OrElse),
        // Comparison
        SyntaxKind.EqualsExpression => nameof(ExpressionType.Equal),
        SyntaxKind.NotEqualsExpression => nameof(ExpressionType.NotEqual),
        SyntaxKind.LessThanExpression => nameof(ExpressionType.LessThan),
        SyntaxKind.LessThanOrEqualExpression => nameof(ExpressionType.LessThanOrEqual),
        SyntaxKind.GreaterThanExpression => nameof(ExpressionType.GreaterThan),
        SyntaxKind.GreaterThanOrEqualExpression => nameof(ExpressionType.GreaterThanOrEqual),
        // Arithmetic
        SyntaxKind.UnaryMinusExpression => nameof(ExpressionType.Negate),
        SyntaxKind.UnaryPlusExpression => nameof(ExpressionType.UnaryPlus),
        SyntaxKind.AddExpression => nameof(ExpressionType.Add),
        SyntaxKind.AddAssignmentExpression => nameof(ExpressionType.AddAssign),
        SyntaxKind.SubtractExpression => nameof(ExpressionType.Subtract),
        SyntaxKind.SubtractAssignmentExpression => nameof(ExpressionType.SubtractAssign),
        SyntaxKind.MultiplyExpression => nameof(ExpressionType.Multiply),
        SyntaxKind.MultiplyAssignmentExpression => nameof(ExpressionType.MultiplyAssign),
        SyntaxKind.DivideExpression => nameof(ExpressionType.Divide),
        SyntaxKind.DivideAssignmentExpression => nameof(ExpressionType.DivideAssign),
        SyntaxKind.ModuloExpression => nameof(ExpressionType.Modulo),
        SyntaxKind.ModuloAssignmentExpression => nameof(ExpressionType.ModuloAssign),
        // Bitwise
        SyntaxKind.BitwiseNotExpression => nameof(ExpressionType.Not),
        SyntaxKind.BitwiseAndExpression => nameof(ExpressionType.And),
        SyntaxKind.BitwiseOrExpression => nameof(ExpressionType.Or),
        SyntaxKind.ExclusiveOrExpression => nameof(ExpressionType.ExclusiveOr),
        SyntaxKind.ExclusiveOrAssignmentExpression => nameof(ExpressionType.ExclusiveOrAssign),
        SyntaxKind.LeftShiftExpression => nameof(ExpressionType.LeftShift),
        SyntaxKind.LeftShiftAssignmentExpression => nameof(ExpressionType.LeftShiftAssign),
        SyntaxKind.RightShiftExpression => nameof(ExpressionType.RightShift),
        SyntaxKind.RightShiftAssignmentExpression => nameof(ExpressionType.RightShiftAssign),
        _ => UnsupportedSyntax(syntax)
    };

    public string CreateDefaultValue(ITypeSymbol type) {
        if(type is { IsAnonymousType: true }) {
            var anonymousProperties = type.GetMembers().OfType<IPropertySymbol>()
            .MkString(p => $"{p.Name} = {CreateDefaultValue(p.Type)}", ", ");

            return $"new {{ {anonymousProperties} }}";
        }

        return $"default({CreateBoundTypeName(type)})";
    }

    private string CreateBoundTypeName(ITypeSymbol type) {
        var unqualified = CreateBoundTypeNameUnqualified(type);

        if(type.ContainingType is not null)
            return $"{CreateBoundTypeName(type.ContainingType)}.{unqualified}";

        if(type.ContainingNamespace is not null)
            return $"{CreateNamespaceName(type.ContainingNamespace)}.{unqualified}";

        return unqualified;
    }

    private string CreateBoundTypeNameUnqualified(ITypeSymbol type) {
        switch(type) {
            case INamedTypeSymbol { IsGenericType: true } generic:
                return $"{type.Name.Substring(0, type.Name.LastIndexOf('`'))}<{generic.TypeArguments.MkString(CreateBoundTypeName, ", ")}>";

            case INamedTypeSymbol named:
                return type.Name;

            default:
                return UnsupportedSymbol(type);
        }
    }

    public string CreateType(ITypeSymbol type) {
        var cacheKey = (LocalDefinitionType.Type, type);
        if(_localRegistrations.TryGetValue(cacheKey, out var cached)) {
            if(cached.Declaration is null)
                return UnsupportedSymbol(type);

            return cached.Identifier;
        }

        var typeIndex = _localRegistrations.Values.Count(r => r.Type == LocalDefinitionType.Type);
        var definition = new LocalDefinition(LocalDefinitionType.Type, $"t{typeIndex}", _localRegistrations.Count);
        _localRegistrations[cacheKey] = definition;
        try {
            definition.Declaration = CreateTypeUncached(type);
            return definition.Identifier;
        } catch {
            _localRegistrations.Remove(cacheKey);
            throw;
        }
    }

    private string CreateTypeUncached(ITypeSymbol type) {
        switch(type) {
            case INamedTypeSymbol { IsGenericType: true } generic:
                return string.Concat(
                    $"typeof({CreateUnboundTypeName(generic)})",
                    $".MakeGenericType({TypeSymbolHelpers.GetInheritedTypeArguments(generic).MkString(CreateType, ", ")})"
                );

            case INamedTypeSymbol named:
                return $"typeof({CreateUnboundTypeName(named)})";

            case { IsAnonymousType: true }:
                return $"{CreateDefaultValue(type)}.GetType()";

            default:
                return UnsupportedSymbol(type);
        }
    }

    private string CreateUnboundTypeName(INamedTypeSymbol type) {
        var unqualified = CreateUnboundTypeNameUnqualified(type);

        if(type.ContainingType is not null)
            return $"{CreateUnboundTypeName(type.ContainingType)}.{unqualified}";

        if(type.ContainingNamespace is not null)
            return $"{CreateNamespaceName(type.ContainingNamespace)}.{unqualified}";

        return $"global::{unqualified}";
    }

    private string CreateUnboundTypeNameUnqualified(INamedTypeSymbol type) {
        switch(type) {
            case { IsGenericType: true }:
                return $"{type.Name.Substring(0, type.Name.LastIndexOf('`'))}<{new string(',', type.TypeArguments.Length - 1)}>";

            default:
                return type.Name;
        }
    }

    private string CreateNamespaceName(INamespaceSymbol ns) => ns switch {
        { ContainingNamespace: { IsGlobalNamespace: true } } => $"global::{ns.Name}",
        _ => $"{CreateNamespaceName(ns.ContainingNamespace)}.{ns.Name}"
    };

    public string CreateTypeArray(IEnumerable<ITypeSymbol> types) =>
        types.MkString("new global::System.Reflection.Type[] {", CreateType, ", ", "}");

    public string CreateMethodInfo(IMethodSymbol method) {
        var cacheKey = (LocalDefinitionType.MethodInfo, method);
        if(_localRegistrations.TryGetValue(cacheKey, out var cached))
            return cached.Identifier;

        var methodIndex = _localRegistrations.Values.Count(d => d.Type == LocalDefinitionType.MethodInfo);
        var definition = new LocalDefinition(LocalDefinitionType.MethodInfo, $"mi{methodIndex}", _localRegistrations.Count);
        definition.Declaration = CreateMethodInfoUncached(method);

        return definition.Identifier;
    }

    private string CreateMethodInfoUncached(IMethodSymbol method) {
        switch(method) {
            case { IsGenericMethod: false }:
                return $"{CreateType(method.ContainingType)}.GetMethod({CreateTypeArray(method.Parameters.Select(p => p.Type))})";

            default:
                return UnsupportedSymbol(method);
        }
    }
}
