using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

public class InterpolationInterceptorGeneratorTestResults(
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
