using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public sealed class InterpolationDiagnosticsCollector(Location defaultLocation, DiagnosticSeverity? severityOverride) {
    private List<Diagnostic> _diagnostics = new();

    public IReadOnlyList<Diagnostic> CollectedDiagnostics => _diagnostics;

    private InterpolatedTree Add(DiagnosticDescriptor descriptor, Location location) {
        _diagnostics.Add(severityOverride switch {
            null => Diagnostic.Create(descriptor, location),
            not null => Diagnostic.Create(descriptor, location, severityOverride.Value)
        });


        return InterpolatedTree.Unsupported;
    }

    public InterpolatedTree UnsupportedInterpolatedSyntax(SyntaxNode node) =>
        Add(InterpolationDiagnostics.UnsupportedInterpolatedSyntax(severityOverride, node), node.GetLocation());

    public InterpolatedTree UnsupportedInvocationSyntax(SyntaxNode node) =>
        Add(InterpolationDiagnostics.UnsupportedInvocationSyntax(severityOverride, node), node.GetLocation());

    public InterpolatedTree UnsupportedEvaluatedSyntax(SyntaxNode node) =>
        Add(InterpolationDiagnostics.UnsupportedEvaluatedSyntax(severityOverride, node), node.GetLocation());

    public InterpolatedTree UnsupportedType(ITypeSymbol typeSymbol, SyntaxNode? node) =>
        Add(InterpolationDiagnostics.UnsupportedType(severityOverride, typeSymbol), node?.GetLocation() ?? defaultLocation);

    public InterpolatedTree ClosureOverScopeReference(IdentifierNameSyntax node) =>
        Add(InterpolationDiagnostics.ClosureOverScopeReference(severityOverride, node), node.GetLocation());

    public InterpolatedTree EvaluatedParameter(IdentifierNameSyntax node) =>
        Add(InterpolationDiagnostics.EvaluatedParameter(severityOverride, node), node.GetLocation());

    public InterpolatedTree NoSplices(SyntaxNode node) =>
        Add(InterpolationDiagnostics.NoSplices(severityOverride, node), node.GetLocation());

    public InterpolatedTree InaccessibleSymbol(ISymbol symbol, SyntaxNode? node) =>
        Add(InterpolationDiagnostics.InaccessibleSymbol(severityOverride, symbol), node?.GetLocation() ?? defaultLocation);

    public InterpolatedTree ReferencesCallSiteTypeParameter(ITypeSymbol symbol, SyntaxNode? node) =>
        Add(InterpolationDiagnostics.ReferencesCallSiteTypeParameter(severityOverride, symbol, node), node?.GetLocation() ?? defaultLocation);
}
