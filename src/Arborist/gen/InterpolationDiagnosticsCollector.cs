using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public sealed class InterpolationDiagnosticsCollector(Location? defaultLocation) {
    private List<Diagnostic> _diagnostics = new();

    public IReadOnlyList<Diagnostic> CollectedDiagnostics => _diagnostics;

    private InterpolatedTree Add(Diagnostic diagnostic) {
        _diagnostics.Add(diagnostic);
        return InterpolatedTree.Unsupported;
    }

    public InterpolatedTree UnsupportedInterpolatedSyntax(SyntaxNode node) =>
        Add(InterpolationDiagnostics.UnsupportedInterpolatedSyntax(node));

    public InterpolatedTree UnsupportedInvocationSyntax(SyntaxNode node) =>
        Add(InterpolationDiagnostics.UnsupportedInvocationSyntax(node));

    public InterpolatedTree UnsupportedEvaluatedSyntax(SyntaxNode node) =>
        Add(InterpolationDiagnostics.UnsupportedEvaluatedSyntax(node));

    public InterpolatedTree UnsupportedType(ITypeSymbol typeSymbol, SyntaxNode? node) =>
        Add(InterpolationDiagnostics.UnsupportedType(typeSymbol, node?.GetLocation() ?? defaultLocation));

    public InterpolatedTree ClosureOverScopeReference(IdentifierNameSyntax node) =>
        Add(InterpolationDiagnostics.ClosureOverScopeReference(node));

    public InterpolatedTree EvaluatedParameter(IdentifierNameSyntax node) =>
        Add(InterpolationDiagnostics.EvaluatedParameter(node));

    public InterpolatedTree NoSplices(SyntaxNode node) =>
        Add(InterpolationDiagnostics.NoSplices(node));

    public InterpolatedTree InaccessibleSymbol(ISymbol symbol, SyntaxNode? node) =>
        Add(InterpolationDiagnostics.InaccessibleSymbol(symbol, node?.GetLocation() ?? defaultLocation));
}
