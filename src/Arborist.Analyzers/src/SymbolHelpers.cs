using Microsoft.CodeAnalysis;

namespace Arborist.Analyzers;

internal static class SymbolHelpers {
    public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeType) {
        foreach(var attribute in symbol.GetAttributes())
            if(SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                return true;

        return false;
    }

    public static bool IsInterpolatedExpressionParameter(
        IParameterSymbol parameter,
        InterpolationTypeSymbols typeSymbols
    ) {
        if(parameter.Type is not INamedTypeSymbol parameterType)
            return false;
        if(!parameterType.IsGenericType)
            return false;
        if(!SymbolEqualityComparer.Default.Equals(parameterType.ConstructUnboundGenericType(), typeSymbols.Expression1.ConstructUnboundGenericType()))
            return false;
        if(parameterType.TypeArguments[0] is not INamedTypeSymbol { IsGenericType: true } interpolatedDelegateType)
            return false;
        if(!IsSubtype(interpolatedDelegateType.TypeArguments[0], typeSymbols.IInterpolationContext))
            return false;
        // Has [InterpolatedExpressionParameter]
        if(!HasAttribute(parameter, typeSymbols.InterpolatedExpressionParameterAttribute))
            return false;

        return true;
    }

    public static bool IsSubtype(ITypeSymbol? a, ITypeSymbol? b) {
        if(a is null)
            return false;
        if(b is null)
            return false;
        if(SymbolEqualityComparer.Default.Equals(a, b))
            return true;

        if(
            a is INamedTypeSymbol { IsGenericType: true } an
            && b is INamedTypeSymbol { IsGenericType: true } bn
            && SymbolEqualityComparer.Default.Equals(an.ConstructUnboundGenericType(), bn.ConstructUnboundGenericType())
        ) {
            for(var i = 0; i < an.TypeArguments.Length; i++) {
                var variance = an.TypeParameters[i].Variance;
                if(VarianceKind.In == variance) {
                    if(!IsSubtype(bn.TypeArguments[i], an.TypeArguments[i]))
                        return false;
                } else if(VarianceKind.Out == variance) {
                    if(!IsSubtype(an.TypeArguments[i], bn.TypeArguments[i]))
                        return false;
                } else {
                    if(!SymbolEqualityComparer.Default.Equals(an.TypeArguments[i], bn.TypeArguments[i]))
                        return false;
                }
            }

            return true;
        }

        if(a.BaseType is not null && IsSubtype(a.BaseType, b))
            return true;

        foreach(var @interface in a.Interfaces)
            if(IsSubtype(@interface, b))
                return true;

        return false;
    }
}
