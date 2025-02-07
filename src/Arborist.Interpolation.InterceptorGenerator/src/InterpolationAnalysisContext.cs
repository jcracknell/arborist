using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public class InterpolationAnalysisContext(
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    InterpolationTypeSymbols typeSymbols,
    InterpolationDiagnosticsCollector diagnostics,
    InterpolatedTreeBuilder treeBuilder,
    LambdaExpressionSyntax interpolatedExpression,
    IParameterSymbol dataParameter,
    IParameterSymbol expressionParameter,
    CancellationToken cancellationToken
) {
    public InvocationExpressionSyntax Invocation { get; } = invocation;
    public Compilation Compilation { get; } = semanticModel.Compilation;
    public SemanticModel SemanticModel { get; } = semanticModel;
    public InterpolationTypeSymbols TypeSymbols { get; } = typeSymbols;
    public InterpolationDiagnosticsCollector Diagnostics { get; } = diagnostics;
    public InterpolatedTreeBuilder TreeBuilder { get; } = treeBuilder;
    public LambdaExpressionSyntax InterpolatedExpression { get; } = interpolatedExpression;
    public IParameterSymbol DataParameter { get; } = dataParameter;
    public IParameterSymbol ExpressionParameter { get; } = expressionParameter;
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// Returns true if the provided <paramref name="node"/> is a reference to the
    /// injected interpolation data.
    /// </summary>
    public bool IsInterpolationDataAccess(MemberAccessExpressionSyntax node) =>
        IsInterpolationDataAccess(SemanticModel.GetSymbolInfo(node).Symbol);

    /// <summary>
    /// Returns true if the provided <paramref name="symbol"/> is a reference to the
    /// injected interpolation data.
    /// </summary>
    public bool IsInterpolationDataAccess(ISymbol? symbol) =>
        symbol is IPropertySymbol { Name: "Data", ContainingType: { IsGenericType: true } } property
        && SymbolEqualityComparer.Default.Equals(
            property.ContainingType.ConstructUnboundGenericType(),
            TypeSymbols.IInterpolationContext1
        );
}
