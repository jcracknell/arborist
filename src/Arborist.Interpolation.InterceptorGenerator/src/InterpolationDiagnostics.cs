using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public static class InterpolationDiagnostics {
    private const string Category = "Arborist.Interpolation";

    public const string ARB998_UnsupportedInterpolatorInvocation = "ARB998";
    public const string ARB997_UnsupportedInterpolatedSyntax = "ARB997";
    public const string ARB996_UnsupportedEvaluatedSyntax = "ARB996";
    public const string ARB995_UnsupportedType = "ARB995";

    public const string ARB000_SetInterpolatorsNamespaces = "ARB000";
    public const string ARB001_InterpolationContextReference = "ARB001";
    public const string ARB002_EvaluatedScopeReference = "ARB002";
    public const string ARB003_EvaluatedInterpolatedParameter = "ARB003";
    public const string ARB004_NoSplices = "ARB004";
    public const string ARB005_InaccessibleSymbolReference = "ARB005";
    public const string ARB006_ReferencesCallSiteTypeParameter = "ARB006";
    public const string ARB007_NonLiteralInterpolatedExpression = "ARB007";

    private static DiagnosticDescriptor Create(
        string code,
        DiagnosticSeverity severity,
        string title,
        string message
    ) =>
        new DiagnosticDescriptor(
            id: code,
            title: title,
            messageFormat: message,
            category: Category,
            defaultSeverity: severity,
            isEnabledByDefault: true
        );

    public static DiagnosticDescriptor SetInterceptorsNamespaces(DiagnosticSeverity? severity) =>
        Create(
            code: ARB000_SetInterpolatorsNamespaces,
            severity: severity ?? DiagnosticSeverity.Info,
            title: $"Add {InterpolationInterceptorGenerator.INTERCEPTOR_NAMESPACE} to the {InterpolationInterceptorGenerator.INTERCEPTORSNAMESPACES_BUILD_PROP} build property",
            message: $"Add {InterpolationInterceptorGenerator.INTERCEPTOR_NAMESPACE} to the {InterpolationInterceptorGenerator.INTERCEPTORSNAMESPACES_BUILD_PROP} build property to enable compile-time expression interpolation."
        );

    public static DiagnosticDescriptor UnsupportedInterpolatedSyntax(DiagnosticSeverity? severity, SyntaxNode node) =>
        Create(
            code: ARB997_UnsupportedInterpolatedSyntax,
            severity: severity ?? DiagnosticSeverity.Info,
            title: "Unsupported Syntax",
            message: $"Syntax node {node} ({node.GetType()}) is not currently supported by compile-time interpolation."
        );

    public static DiagnosticDescriptor UnsupportedInvocationSyntax(DiagnosticSeverity? severity, SyntaxNode node) =>
        Create(
            code: ARB998_UnsupportedInterpolatorInvocation,
            severity: severity ?? DiagnosticSeverity.Warning,
            title: "Unhandled expression interpolator method signature",
            message: ""
        );

    public static DiagnosticDescriptor UnsupportedEvaluatedSyntax(DiagnosticSeverity? severity, SyntaxNode node) =>
        Create(
            code: ARB996_UnsupportedEvaluatedSyntax,
            severity: severity ?? DiagnosticSeverity.Info,
            title: "Unsupported syntax in interpolated expression",
            message: $"Syntax node {node} ({node.GetType()}) is not currently supported by compile-time interpolation."
        );

    public static DiagnosticDescriptor UnsupportedType(DiagnosticSeverity? severity, ITypeSymbol typeSymbol) =>
        Create(
            code: ARB995_UnsupportedType,
            severity: severity ?? DiagnosticSeverity.Info,
            title: "Unsupported type in interpolated expression",
            message: $"Interpolated expression contains unsupported type symbol {typeSymbol} and cannot be interpolated at compile time."
        );

    public static DiagnosticDescriptor EvaluatedScopeReference(DiagnosticSeverity? severity, SyntaxNode node) =>
        Create(
            code: ARB002_EvaluatedScopeReference,
            severity: severity ?? DiagnosticSeverity.Warning,
            title: "Evaluated splice argument references scope identifier",
            message: $"Evaluated splice argument references identifier `{node}` defined in an enclosing scope."
        );

    public static DiagnosticDescriptor EvaluatedInterpolatedIdentifier(DiagnosticSeverity? severity, IdentifierNameSyntax node) =>
        Create(
            code: ARB003_EvaluatedInterpolatedParameter,
            severity: severity ?? DiagnosticSeverity.Error,
            title: "Evaluated splice argument references interpolated identifier",
            message: $"Evaluated splice argument references identifier `{node}` defined in the enclosing interpolated expression."
        );

    public static DiagnosticDescriptor InterpolationContextReference(DiagnosticSeverity? severity, IdentifierNameSyntax node) =>
        Create(
            code: ARB001_InterpolationContextReference,
            severity: severity ?? DiagnosticSeverity.Error,
            title: "Interpolation context reference",
            message: $"Interpolated expression contains a reference to the context parameter `{node}` which is not part of a splicing call."
        );

    public static DiagnosticDescriptor NoSplices(DiagnosticSeverity? severity, SyntaxNode node) =>
        Create(
            code: ARB004_NoSplices,
            severity: severity ?? DiagnosticSeverity.Warning,
            title: "Interpolated expression contains no splices",
            message: $"Interpolated expression contains no splices, and has no effect."
        );

    public static DiagnosticDescriptor InaccessibleSymbol(DiagnosticSeverity? severity, ISymbol symbol) =>
        Create(
            code: ARB005_InaccessibleSymbolReference,
            severity: severity ?? DiagnosticSeverity.Info,
            title: "Evaluated splice argument references inaccessible symbol",
            message: $"Evaluated splice argument references inaccessible symbol {symbol} and cannot be interpolated at compile time."
        );

    public static DiagnosticDescriptor ReferencesCallSiteTypeParameter(DiagnosticSeverity? severity, ITypeSymbol symbol, SyntaxNode? node) =>
        Create(
            code: ARB006_ReferencesCallSiteTypeParameter,
            severity: severity ?? DiagnosticSeverity.Info,
            title: "Evaluated splice argument references call-site type parameter",
            message: $"Evaluated splice argument contains a reference to call-site type parameter {symbol} and cannot be interpolated at compile time."
        );

    public static DiagnosticDescriptor NonLiteralInterpolatedExpression(DiagnosticSeverity? severity) =>
        Create(
            code: ARB007_NonLiteralInterpolatedExpression,
            severity: severity ?? DiagnosticSeverity.Info,
            title: "Interpolated expression was not specified as a literal",
            message: "Interpolated expression was not specified as a literal (inline) expression and cannot be interpolated at compile time."
        );
}
