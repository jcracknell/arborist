using Microsoft.CodeAnalysis;

namespace Arborist.Analyzers;

public class InterpolationAnalysisContext(
    SemanticModel semanticModel,
    InterpolationTypeSymbols typeSymbols,
    InterpolationDiagnosticsCollection diagnostics,
    CancellationToken cancellationToken
) {
    public SemanticModel SemanticModel { get; } = semanticModel;
    public InterpolationTypeSymbols TypeSymbols { get; } = typeSymbols;
    public InterpolationDiagnosticsCollection Diagnostics { get; } = diagnostics;
    public CancellationToken CancellationToken { get; } = cancellationToken;
}
