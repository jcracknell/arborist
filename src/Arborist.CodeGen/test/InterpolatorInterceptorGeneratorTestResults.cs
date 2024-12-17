using Microsoft.CodeAnalysis;

namespace Arborist.CodeGen;

public class InterpolatorInterceptorGeneratorTestResults(
    Compilation compilation,
    IReadOnlyList<InterpolatorAnalysisResults> analysisResults,
    IReadOnlyList<SyntaxTree> generatedTrees,
    IReadOnlyList<Diagnostic> diagnostics
) {
    public Compilation Compilation { get; } = compilation;
    public IReadOnlyList<InterpolatorAnalysisResults> AnalysisResults { get; } = analysisResults;
    public IReadOnlyList<SyntaxTree> GeneratedTrees { get; } = generatedTrees;
    public IReadOnlyList<Diagnostic> Diagnostics { get; } = diagnostics;
}
