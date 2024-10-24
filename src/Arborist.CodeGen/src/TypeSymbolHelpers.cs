using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Arborist.CodeGen;

internal static class TypeSymbolHelpers {
    /// <summary>
    /// Gets the complete set of type arguments for the provided <paramref name="type"/>,
    /// including those provided in containing types.
    /// </summary>
    public static IReadOnlyList<ITypeSymbol> GetInheritedTypeArguments(INamedTypeSymbol type) {
        return Recurse(type);
        static ImmutableList<ITypeSymbol> Recurse(INamedTypeSymbol type) =>
            type.ContainingType is null
            ? ImmutableList.CreateRange(type.TypeArguments)
            : Recurse(type.ContainingType).AddRange(type.TypeArguments);
    }

    public static bool IsSubtype(ITypeSymbol? a, ITypeSymbol? b) {
        if(a is null)
            return false;
        if(b is null)
            return false;
        if(a.Equals(b, SymbolEqualityComparer.Default))
            return true;

        if(
            a is INamedTypeSymbol { IsGenericType: true } an
            && b is INamedTypeSymbol { IsGenericType: true } bn
            && an.ConstructUnboundGenericType().Equals(bn.ConstructUnboundGenericType(), SymbolEqualityComparer.Default)
        ) {
            var typeArgInfo = an.TypeParameters.Zip(
                an.TypeArguments.Zip(bn.TypeArguments, (a, b) => (a, b)),
                (param, args) => (param, args)
            );

            if(typeArgInfo.All(tup => tup.param.Variance switch {
                VarianceKind.In => IsSubtype(tup.args.b, tup.args.a),
                VarianceKind.Out => IsSubtype(tup.args.a, tup.args.b),
                _ => tup.args.a.Equals(tup.args.b, SymbolEqualityComparer.Default)
            }))
                return true;
        }

        if(a.BaseType is not null && IsSubtype(a.BaseType, b))
            return true;

        foreach(var @interface in a.Interfaces)
            if(IsSubtype(@interface, b))
                return true;

        return false;
    }

    public static bool TryGetInterfaceImplementation(INamedTypeSymbol @interface, ITypeSymbol type, out INamedTypeSymbol implementation) =>
        type.AllInterfaces.TryGetSingle(
            i => SymbolEqualityComparer.Default.Equals(@interface, i)
            || i.IsGenericType && @interface.IsGenericType && SymbolEqualityComparer.Default.Equals(@interface, i.ConstructUnboundGenericType()),
            out implementation
        );

    public static string TypeName(INamedTypeSymbol type) {
        if(type.ContainingType is not null)
            return $"{TypeName(type.ContainingType)}.{type.Name}";

        if(type.ContainingNamespace is not null)
            return $"{NamespaceName(type.ContainingNamespace)}.{type.Name}";

        return type.Name;
    }

    public static string NamespaceName(INamespaceSymbol ns) =>
        ns switch {
            { ContainingNamespace: { IsGlobalNamespace: true } } => $"global::{ns.Name}",
            _ => $"{NamespaceName(ns.ContainingNamespace)}.{ns.Name}"
        };
}
