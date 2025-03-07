using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

public sealed class InterpolationTypeSymbols {
    private const int MAX_DELEGATE_PARAMETER_COUNT = 5;

    public static InterpolationTypeSymbols Create(Compilation compilation) =>
        new(compilation);

    private InterpolationTypeSymbols(Compilation compilation) {
        IInterpolationContext = compilation.GetTypeByMetadataName("Arborist.Interpolation.IInterpolationContext")!;
        IInterpolationContext1 = compilation.GetTypeByMetadataName("Arborist.Interpolation.IInterpolationContext`1")!.ConstructUnboundGenericType();
        InterceptedExpressionInterpolatorAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.InterceptedExpressionInterpolatorAttribute")!;
        EvaluatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.EvaluatedSpliceParameterAttribute")!;
        InterpolatedSpliceParameterAttribute = compilation.GetTypeByMetadataName("Arborist.Interpolation.Internal.InterpolatedSpliceParameterAttribute")!;

        Expression = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression")!;
        Expression1 = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1")!.ConstructUnboundGenericType();
        ConstantExpression = compilation.GetTypeByMetadataName("System.Linq.Expressions.ConstantExpression")!;

        Nullable = compilation.GetTypeByMetadataName("System.Nullable`1")!.ConstructUnboundGenericType();
        Object = compilation.GetTypeByMetadataName("System.Object")!;
        String = compilation.GetTypeByMetadataName("System.String")!;

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

    public INamedTypeSymbol InterceptedExpressionInterpolatorAttribute { get; }
    public INamedTypeSymbol IInterpolationContext { get; }
    public INamedTypeSymbol IInterpolationContext1 { get; }
    public INamedTypeSymbol EvaluatedSpliceParameterAttribute { get; }
    public INamedTypeSymbol InterpolatedSpliceParameterAttribute { get; }
    public INamedTypeSymbol Expression { get; }
    public INamedTypeSymbol Expression1 { get; }
    public INamedTypeSymbol ConstantExpression { get; }
    public INamedTypeSymbol Nullable { get; }
    public INamedTypeSymbol Object { get; }
    public INamedTypeSymbol String { get; }

    public ImmutableArray<INamedTypeSymbol> Actions { get; }
    public ImmutableArray<INamedTypeSymbol> Funcs { get; }
}
