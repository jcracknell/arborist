using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;

namespace Arborist.Analyzers;

public sealed class InterpolationDiagnosticsCollection : IReadOnlyCollection<Diagnostic> {
    private readonly List<Diagnostic> _diagnostics = new();

    public int Count => _diagnostics.Count;
    public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _diagnostics.GetEnumerator();

    public void ReportUnsupportedSyntax(SyntaxNode node) {
        _diagnostics.Add(Diagnostic.Create(
            descriptor: InterpolationDiagnosticDescriptors.ARB999_UnsupportedSyntax,
            location: node.GetLocation()
        ));
    }

    public void ReportNoSplices(LambdaExpressionSyntax node) {
        _diagnostics.Add(Diagnostic.Create(
            descriptor: InterpolationDiagnosticDescriptors.ARB001_NoSplices,
            location: node.GetLocation()
        ));
    }

    public void ReportInterpolationContextReference(SyntaxNode node) {
        _diagnostics.Add(Diagnostic.Create(
            descriptor: InterpolationDiagnosticDescriptors.ARB002_InterpolationContextReference,
            location: node.GetLocation()
        ));
    }

    public void ReportInterpolatedParameterReference(SyntaxNode node) {
        _diagnostics.Add(Diagnostic.Create(
            descriptor: InterpolationDiagnosticDescriptors.ARB003_InterpolatedParameterReference,
            location: node.GetLocation()
        ));
    }

    public void ReportNestedInterpolation(InvocationExpressionSyntax node) {
        _diagnostics.Add(Diagnostic.Create(
            descriptor: InterpolationDiagnosticDescriptors.ARB004_NestedInterpolation,
            location: node.GetLocation()
        ));
    }
}
