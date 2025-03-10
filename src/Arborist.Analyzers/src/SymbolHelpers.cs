using Microsoft.CodeAnalysis;

namespace Arborist.Analyzers;

internal static class SymbolHelpers {
    public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeType) {
        foreach(var attribute in symbol.GetAttributes())
            if(SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                return true;

        return false;
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

    public static bool TryGetInterfaceImplementation(
        INamedTypeSymbol @interface,
        ITypeSymbol type,
        [MaybeNullWhen(false)] out INamedTypeSymbol implementation
    ) =>
        type.AllInterfaces.Prepend(type).OfType<INamedTypeSymbol>().TryGetSingle(
            i => SymbolEqualityComparer.Default.Equals(@interface, i)
            || i.IsGenericType && @interface.IsGenericType && SymbolEqualityComparer.Default.Equals(@interface, i.ConstructUnboundGenericType()),
            out implementation
        );
}
