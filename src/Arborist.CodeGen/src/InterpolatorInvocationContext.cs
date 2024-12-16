using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public class InterpolatorInvocationContext(
    SourceProductionContext sourceProductionContext,
    Compilation compilation,
    InterpolatorTypeSymbols typeSymbols,
    DiagnosticFactory diagnostics,
    InterpolatedTreeBuilder builder,
    InvocationExpressionSyntax invocationSyntax,
    IMethodSymbol methodSymbol,
    IParameterSymbol? dataParameter,
    IParameterSymbol expressionParameter,
    LambdaExpressionSyntax interpolatedExpression,
    IReadOnlyList<ParameterSyntax> interpolatedExpressionParameters
) {
    public SourceProductionContext SourceProductionContext { get; } = sourceProductionContext;
    public Compilation Compilation { get; } = compilation;
    public InterpolatorTypeSymbols TypeSymbols { get; } = typeSymbols;
    public DiagnosticFactory Diagnostics { get; } = diagnostics;
    public InterpolatedTreeBuilder Builder { get; } = builder;
    public SemanticModel SemanticModel { get; } = compilation.GetSemanticModel(invocationSyntax.SyntaxTree);
    public InvocationExpressionSyntax InvocationSyntax { get; } = invocationSyntax;
    public IMethodSymbol MethodSymbol { get; } = methodSymbol;
    public IParameterSymbol? DataParameter { get; } = dataParameter;
    public IParameterSymbol ExpressionParameter { get; } = expressionParameter;
    public LambdaExpressionSyntax InterpolatedExpression { get; } = interpolatedExpression;
    public IReadOnlyList<ParameterSyntax> InterpolatedExpressionParameters { get; } = interpolatedExpressionParameters;

    public int SpliceCount { get; set; }

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
