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
    public const string ARB001_ClosureOverScopeReference = "ARB001";
    public const string ARB002_EvaluatedInterpolatedParameter = "ARB002";
    public const string ARB003_NoSplices = "ARB003";
    public const string ARB004_InaccessibleSymbolReference = "ARB004";
    public const string ARB005_ReferencesCallSiteTypeParameter = "ARB005";

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

    public static DiagnosticDescriptor ClosureOverScopeReference(DiagnosticSeverity? severity, IdentifierNameSyntax node) =>
        Create(
            code: ARB001_ClosureOverScopeReference,
            severity: severity ?? DiagnosticSeverity.Warning,
            title: "Closure",
            message: $"Interpolated expression closes over identifier `{node}` defined in an enclosing scope."
        );

    public static DiagnosticDescriptor EvaluatedParameter(DiagnosticSeverity? severity, IdentifierNameSyntax node) =>
        Create(
            code: ARB002_EvaluatedInterpolatedParameter,
            severity: severity ?? DiagnosticSeverity.Error,
            title: "Evaluated Parameter",
            message: $"Evaluated splice argument references identifier `{node}` defined in the enclosing interpolated expression."
        );

    public static DiagnosticDescriptor NoSplices(DiagnosticSeverity? severity, SyntaxNode node) =>
        Create(
            code: ARB003_NoSplices,
            severity: severity ?? DiagnosticSeverity.Warning,
            title: "Interpolated expression contains no splices",
            message: $"Interpolated expression contains no splices, and has no effect."
        );

    public static DiagnosticDescriptor InaccessibleSymbol(DiagnosticSeverity? severity, ISymbol symbol) =>
        Create(
            code: ARB004_InaccessibleSymbolReference,
            severity: severity ?? DiagnosticSeverity.Info,
            title: "Inaccesible Symbol Reference",
            message: $"Interpolated expression references inaccessible symbol {symbol} and cannot be interpolated at compile time."
        );

    public static DiagnosticDescriptor ReferencesCallSiteTypeParameter(DiagnosticSeverity? severity, ITypeSymbol symbol, SyntaxNode? node) =>
        Create(
            code: ARB005_ReferencesCallSiteTypeParameter,
            severity: severity ?? DiagnosticSeverity.Info,
            title: "Interpolated expression references call-site type parameter",
            message: $"The interpolated expression contains a reference to call-site type parameter {symbol} and cannot be interpolated at compile time."
        );
}
