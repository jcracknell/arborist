using Microsoft.CodeAnalysis;

namespace Arborist.Analyzers;

public sealed class InterpolationTypeSymbols {
    public static InterpolationTypeSymbols Create(Compilation compilation) =>
        new(compilation);

    private InterpolationTypeSymbols(Compilation compilation) {
        IInterpolationContext = compilation.GetTypeByMetadataName("Arborist.Interpolation.IInterpolationContext")!;
        IInterpolationContext1 = compilation.GetTypeByMetadataName("Arborist.Interpolation.IInterpolationContext`1")!.ConstructUnboundGenericType();
        ExpressionInterpolatorAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.ExpressionInterpolatorAttribute")!;
        InterpolatedExpressionParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.InterpolatedExpressionParameterAttribute")!;
        EvaluatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.EvaluatedSpliceParameterAttribute")!;
        InterpolatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.InterpolatedSpliceParameterAttribute")!;
        SplicingOperations = compilation.GetTypeByMetadataName("Arborist.SplicingOperations")!;
        Expression1 = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1")!.ConstructUnboundGenericType();
    }

    public INamedTypeSymbol ExpressionInterpolatorAttribute { get; }
    public INamedTypeSymbol InterpolatedExpressionParameterAttribute { get; }
    public INamedTypeSymbol IInterpolationContext { get; }
    public INamedTypeSymbol IInterpolationContext1 { get; }
    public INamedTypeSymbol EvaluatedSpliceParameterAttribute { get; }
    public INamedTypeSymbol InterpolatedSpliceParameterAttribute { get; }
    public INamedTypeSymbol SplicingOperations { get;}
    public INamedTypeSymbol Expression1 { get; }
}
