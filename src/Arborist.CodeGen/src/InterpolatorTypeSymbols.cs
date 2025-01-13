using Microsoft.CodeAnalysis;

namespace Arborist.CodeGen;

public sealed class InterpolatorTypeSymbols {
    public InterpolatorTypeSymbols(Compilation compilation) {
        IInterpolationContext = compilation.GetTypeByMetadataName("Arborist.Interpolation.IInterpolationContext")!;
        IInterpolationContext1 = compilation.GetTypeByMetadataName("Arborist.Interpolation.IInterpolationContext`1")!.ConstructUnboundGenericType();
        ExpressionInterpolatorAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.ExpressionInterpolatorAttribute")!;
        EvaluatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.EvaluatedSpliceParameterAttribute")!;
        InterpolatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.InterpolatedSpliceParameterAttribute")!;

        Expression = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression")!;
        Expression1 = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1")!.ConstructUnboundGenericType();

        Nullable = compilation.GetTypeByMetadataName("System.Nullable`1")!.ConstructUnboundGenericType();
        Object = compilation.GetTypeByMetadataName("System.Object")!;
        String = compilation.GetTypeByMetadataName("System.String")!;

        Actions = ImmutableArray.CreateRange(
            from n in Enumerable.Range(0, InterpolatorInterceptorGenerator.MAX_DELEGATE_PARAMETER_COUNT + 1)
            select n switch {
                0 => compilation.GetTypeByMetadataName("System.Action")!,
                _ => compilation.GetTypeByMetadataName($"System.Action`{n}")!.ConstructUnboundGenericType()
            }
        );

        Funcs = ImmutableArray.CreateRange(
            from n in Enumerable.Range(0, InterpolatorInterceptorGenerator.MAX_DELEGATE_PARAMETER_COUNT + 2)
            select compilation.GetTypeByMetadataName($"System.Func`{n + 1}")!.ConstructUnboundGenericType()
        );
    }

    public INamedTypeSymbol ExpressionInterpolatorAttribute { get; }
    public INamedTypeSymbol IInterpolationContext { get; }
    public INamedTypeSymbol IInterpolationContext1 { get; }
    public INamedTypeSymbol EvaluatedSpliceParameterAttribute { get; }
    public INamedTypeSymbol InterpolatedSpliceParameterAttribute { get; }
    public INamedTypeSymbol Expression { get; }
    public INamedTypeSymbol Expression1 { get; }
    public INamedTypeSymbol Nullable { get; }
    public INamedTypeSymbol Object { get; }
    public INamedTypeSymbol String { get; }

    public ImmutableArray<INamedTypeSymbol> Actions { get; }
    public ImmutableArray<INamedTypeSymbol> Funcs { get; }
}
