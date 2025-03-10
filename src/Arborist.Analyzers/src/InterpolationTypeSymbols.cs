using Microsoft.CodeAnalysis;

namespace Arborist.Analyzers;

public sealed class InterpolationTypeSymbols {
    private const int MAX_DELEGATE_PARAMETER_COUNT = 5;

    public static InterpolationTypeSymbols Create(Compilation compilation) =>
        new(compilation);

    private InterpolationTypeSymbols(Compilation compilation) {
        IInterpolationContext = compilation.GetTypeByMetadataName("Arborist.Interpolation.IInterpolationContext")!;
        IInterpolationContext1 = compilation.GetTypeByMetadataName("Arborist.Interpolation.IInterpolationContext`1")!.ConstructUnboundGenericType();
        ExpressionInterpolatorAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.ExpressionInterpolatorAttribute")!;
        EvaluatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.EvaluatedSpliceParameterAttribute")!;
        InterpolatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.InterpolatedSpliceParameterAttribute")!;

        Expression1 = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1")!.ConstructUnboundGenericType();

        Actions = ImmutableArray.CreateRange(
            from n in Enumerable.Range(0, MAX_DELEGATE_PARAMETER_COUNT + 1)
            select n switch {
                0 => compilation.GetTypeByMetadataName("System.Action")!,
                _ => compilation.GetTypeByMetadataName($"System.Action`{n}")!.ConstructUnboundGenericType()
            }
        );

        Funcs = ImmutableArray.CreateRange(
            from n in Enumerable.Range(0, MAX_DELEGATE_PARAMETER_COUNT + 2)
            select compilation.GetTypeByMetadataName($"System.Func`{n + 1}")!.ConstructUnboundGenericType()
        );
    }

    public INamedTypeSymbol ExpressionInterpolatorAttribute { get; }
    public INamedTypeSymbol IInterpolationContext { get; }
    public INamedTypeSymbol IInterpolationContext1 { get; }
    public INamedTypeSymbol EvaluatedSpliceParameterAttribute { get; }
    public INamedTypeSymbol InterpolatedSpliceParameterAttribute { get; }
    public INamedTypeSymbol Expression1 { get; }

    public ImmutableArray<INamedTypeSymbol> Actions { get; }
    public ImmutableArray<INamedTypeSymbol> Funcs { get; }
}
