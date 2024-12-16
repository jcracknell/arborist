using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public sealed class DiagnosticFactory(
    SourceProductionContext sourceProductionContext,
    InvocationExpressionSyntax invocationSyntax
) {
    public const string ARB999_UnsupportedInterpolatorInvocation = "ARB999";
    public const string ARB998_UnsupportedInterpolatedSyntax = "ARB999";
    public const string ARB997_UnsupportedEvaluatedSyntax = "ARB997";
    public const string ARB996_UnsupportedType = "ARB996";

    public const string ARB001_ClosureOverScopeReference = "ARB001";
    public const string ARB002_EvaluatedInterpolatedParameter = "ARB002";
    public const string ARB003_NoSplices = "ARB003";
    public const string ARB004_InaccessibleSymbolReference = "ARB004";

    private A Diagnostic<A>(
        A result,
        string code,
        DiagnosticSeverity severity,
        string title,
        string message,
        SyntaxNode? syntax = default
    ) {
        sourceProductionContext.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(
            descriptor: new DiagnosticDescriptor(
                id: code,
                title: title,
                messageFormat: message,
                category: "ExpressionInterpolation",
                defaultSeverity: severity,
                isEnabledByDefault: true
            ),
            location: (syntax ?? invocationSyntax).GetLocation()
        ));
        return result;
    }

    public InterpolatedTree UnsupportedInterpolatedSyntax(SyntaxNode node) =>
        Diagnostic(
            result: InterpolatedTree.Unsupported,
            code: ARB998_UnsupportedInterpolatedSyntax,
            severity: DiagnosticSeverity.Info,
            title: "Unsupported Syntax",
            message: $"Syntax node {node} ({node.GetType()}) is not currently supported by compile-time interpolation.",
            syntax: node
        );

    public InterpolatedTree UnsupportedEvaluatedSyntax(SyntaxNode node) =>
        Diagnostic(
            result: InterpolatedTree.Unsupported,
            code: ARB997_UnsupportedEvaluatedSyntax,
            severity: DiagnosticSeverity.Info,
            title: "Unsupported Syntax",
            message: $"Syntax node {node} ({node.GetType()}) is not currently supported by compile-time interpolation.",
            syntax: node
        );

    public InterpolatedTree UnsupportedType(ITypeSymbol typeSymbol, SyntaxNode? syntax) =>
        Diagnostic(
            result: InterpolatedTree.Unsupported,
            code: ARB996_UnsupportedType,
            severity: DiagnosticSeverity.Info,
            title: "Unsupported Type",
            message: $"Interpolated expression contains unsupported type symbol {typeSymbol} and cannot be interpolated at compile time.",
            syntax: syntax
        );

    public InterpolatedTree Closure(IdentifierNameSyntax node) =>
        Diagnostic(
            result: InterpolatedTree.Unsupported,
            code: ARB001_ClosureOverScopeReference,
            severity: DiagnosticSeverity.Warning,
            title: "Closure",
            message: $"Interpolated expression closes over identifier `{node}` defined in an enclosing scope.",
            syntax: node
        );

    public InterpolatedTree EvaluatedParameter(IdentifierNameSyntax node) =>
        Diagnostic(
            result: InterpolatedTree.Unsupported,
            code: ARB002_EvaluatedInterpolatedParameter,
            severity: DiagnosticSeverity.Error,
            title: "Evaluated Parameter",
            message: $"Evaluated splice argument references identifier `{node}` defined in the enclosing interpolated expression.",
            syntax: node
        );

    public InterpolatedTree NoSplices(SyntaxNode node) =>
        Diagnostic(
            result: InterpolatedTree.Unsupported,
            code: ARB003_NoSplices,
            severity: DiagnosticSeverity.Warning,
            title: "Interpolated expression contains no splices",
            message: $"Interpolated expression contains no splices, and has no effect.",
            syntax: node
        );

    public InterpolatedTree InaccesibleSymbol(ISymbol symbol, SyntaxNode? node) =>
        Diagnostic(
            result: InterpolatedTree.Unsupported,
            code: ARB004_InaccessibleSymbolReference,
            severity: DiagnosticSeverity.Info,
            title: "Inaccesible Symbol Reference",
            message: $"Interpolated expression references inaccessible symbol {symbol} and cannot be interpolated at compile time.",
            syntax: node
        );
}
