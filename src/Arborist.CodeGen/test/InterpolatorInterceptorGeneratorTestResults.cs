using Microsoft.CodeAnalysis;

namespace Arborist.CodeGen;

public class InterpolatorInterceptorGeneratorTestResults(
    Compilation compilation,
    IReadOnlyList<InterpolationAnalysisResult> analysisResults,
    IReadOnlyList<SyntaxTree> generatedTrees,
    IReadOnlyList<Diagnostic> diagnostics
) {
    public Compilation Compilation { get; } = compilation;
    public IReadOnlyList<InterpolationAnalysisResult> AnalysisResults { get; } = analysisResults;
    public IReadOnlyList<SyntaxTree> GeneratedTrees { get; } = generatedTrees;
    public IReadOnlyList<Diagnostic> Diagnostics { get; } = diagnostics;
}
