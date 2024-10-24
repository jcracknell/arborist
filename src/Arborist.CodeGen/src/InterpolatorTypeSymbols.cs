using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Arborist.CodeGen;

internal sealed class InterpolatorTypeSymbols {
    public InterpolatorTypeSymbols(Compilation compilation) {
        IInterpolationContext = compilation.GetTypeByMetadataName("Arborist.Interpolation.InterpolationContext")!;
        IInterpolationContext1 = compilation.GetTypeByMetadataName("Arborist.Interpolation.InterpolationContext`1")!;

        ExpressionInterpolatorAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.ExpressionInterpolatorAttribute")!;
        EvaluatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.EvaluatedSpliceParameterAttribute")!;
        InterpolatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.InterpolatedSpliceParameterAttribute")!;

        Expression1 = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1")!;

        Actions = ImmutableArray.CreateRange(
            from n in Enumerable.Range(0, InterpolatorInterceptorGenerator.MAX_DELEGATE_PARAMETER_COUNT)
            select n switch {
                0 => compilation.GetTypeByMetadataName("System.Action")!,
                _ => compilation.GetTypeByMetadataName($"System.Action`{n}")!
            }
        );

        Funcs = ImmutableArray.CreateRange(
            from n in Enumerable.Range(0, InterpolatorInterceptorGenerator.MAX_DELEGATE_PARAMETER_COUNT)
            select compilation.GetTypeByMetadataName($"System.Func`{n + 1}")
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
