using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Analyzers;

public class InterpolationAnalysisContext(
    InvocationExpressionSyntax invocation,
    LambdaExpressionSyntax lambdaSyntax,
    SemanticModel semanticModel,
    InterpolationTypeSymbols typeSymbols,
    InterpolationDiagnosticsCollection diagnostics,
    CancellationToken cancellationToken
) {
    public InvocationExpressionSyntax Invocation { get; } = invocation;
    public LambdaExpressionSyntax LambdaSyntax { get; } = lambdaSyntax;
    public SemanticModel SemanticModel { get; } = semanticModel;
    public InterpolationTypeSymbols TypeSymbols { get; } = typeSymbols;
    public InterpolationDiagnosticsCollection Diagnostics { get; } = diagnostics;
    public CancellationToken CancellationToken { get; } = cancellationToken;
}
