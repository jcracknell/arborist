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

    private static Diagnostic Create(
        string code,
        DiagnosticSeverity severity,
        string title,
        string message,
        Location? location
    ) =>
        Diagnostic.Create(
            descriptor: new DiagnosticDescriptor(
                id: code,
                title: title,
                messageFormat: message,
                category: Category,
                defaultSeverity: severity,
                isEnabledByDefault: true
            ),
            location: location
        );

    public static Diagnostic SetInterceptorsNamespaces(Location location) =>
        Create(
            code: ARB000_SetInterpolatorsNamespaces,
            severity: DiagnosticSeverity.Info,
            title: $"Add {InterpolationInterceptorGenerator.INTERCEPTOR_NAMESPACE} to the {InterpolationInterceptorGenerator.INTERCEPTORSNAMESPACES_BUILD_PROP} build property",
            message: $"Add {InterpolationInterceptorGenerator.INTERCEPTOR_NAMESPACE} to the {InterpolationInterceptorGenerator.INTERCEPTORSNAMESPACES_BUILD_PROP} build property to enable compile-time expression interpolation.",
            location: location
        );

    public static Diagnostic UnsupportedInterpolatedSyntax(SyntaxNode node) =>
        Create(
            code: ARB997_UnsupportedInterpolatedSyntax,
            severity: DiagnosticSeverity.Info,
            title: "Unsupported Syntax",
            message: $"Syntax node {node} ({node.GetType()}) is not currently supported by compile-time interpolation.",
            location: node.GetLocation()
        );

    public static Diagnostic UnsupportedInvocationSyntax(SyntaxNode node) =>
        Create(
            code: ARB998_UnsupportedInterpolatorInvocation,
            severity: DiagnosticSeverity.Warning,
            title: "Unhandled expression interpolator method signature",
            message: "",
            location: node.GetLocation()
        );

    public static Diagnostic UnsupportedEvaluatedSyntax(SyntaxNode node) =>
        Create(
            code: ARB996_UnsupportedEvaluatedSyntax,
            severity: DiagnosticSeverity.Info,
            title: "Unsupported syntax in interpolated expression",
            message: $"Syntax node {node} ({node.GetType()}) is not currently supported by compile-time interpolation.",
            location: node.GetLocation()
        );

    public static Diagnostic UnsupportedType(ITypeSymbol typeSymbol, Location? location) =>
        Create(
            code: ARB995_UnsupportedType,
            severity: DiagnosticSeverity.Info,
            title: "Unsupported type in interpolated expression",
            message: $"Interpolated expression contains unsupported type symbol {typeSymbol} and cannot be interpolated at compile time.",
            location: location
        );

    public static Diagnostic ClosureOverScopeReference(IdentifierNameSyntax node) =>
        Create(
            code: ARB001_ClosureOverScopeReference,
            severity: DiagnosticSeverity.Warning,
            title: "Closure",
            message: $"Interpolated expression closes over identifier `{node}` defined in an enclosing scope.",
            location: node.GetLocation()
        );

    public static Diagnostic EvaluatedParameter(IdentifierNameSyntax node) =>
        Create(
            code: ARB002_EvaluatedInterpolatedParameter,
            severity: DiagnosticSeverity.Error,
            title: "Evaluated Parameter",
            message: $"Evaluated splice argument references identifier `{node}` defined in the enclosing interpolated expression.",
            location: node.GetLocation()
        );

    public static Diagnostic NoSplices(SyntaxNode node) =>
        Create(
            code: ARB003_NoSplices,
            severity: DiagnosticSeverity.Warning,
            title: "Interpolated expression contains no splices",
            message: $"Interpolated expression contains no splices, and has no effect.",
            location: node.GetLocation()
        );

    public static Diagnostic InaccessibleSymbol(ISymbol symbol, Location? location) =>
        Create(
            code: ARB004_InaccessibleSymbolReference,
            severity: DiagnosticSeverity.Info,
            title: "Inaccesible Symbol Reference",
            message: $"Interpolated expression references inaccessible symbol {symbol} and cannot be interpolated at compile time.",
            location: location
        );
}
