using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

internal static partial class TypeSymbolHelpers {
    /// <summary>
    /// Replaces occurrences of the provided <paramref name="replacements"/> appearing in the provided
    /// <paramref name="typeSymbol"/>, including type parameters in the event that the provided
    /// <paramref name="typeSymbol"/> is a generic type.
    /// </summary>
    /// <remarks>
    /// The provided <paramref name="replacements"/> dictionary should use the
    /// <see cref="SymbolEqualityComparer.IncludeNullability"/> key comparer, and should have keys with
    /// a <see cref="NullableAnnotation"/> value of <see cref="NullableAnnotation.None"/>.
    /// </remarks>
    public static ITypeSymbol ReplaceTypes(
        ITypeSymbol typeSymbol,
        IReadOnlyDictionary<ITypeSymbol, ITypeSymbol> replacements
    ) {
        if(replacements.Count == 0)
            return typeSymbol;

        if(replacements.TryGetValue(typeSymbol.WithNullableAnnotation(NullableAnnotation.None), out var replacement))
            return replacement.WithNullableAnnotation(typeSymbol.NullableAnnotation);

        if(typeSymbol is INamedTypeSymbol { IsGenericType: true } generic)
            return generic.ConstructedFrom.Construct(
                typeArguments: ImmutableArray.CreateRange(generic.TypeArguments.Select(ta => ReplaceTypes(ta, replacements))),
                typeArgumentNullableAnnotations: generic.TypeArgumentNullableAnnotations
            )
            .WithNullableAnnotation(typeSymbol.NullableAnnotation);

        return typeSymbol;
    }

    public static ITypeSymbol GetRootArrayElementType(IArrayTypeSymbol arrayType) =>
        arrayType.ElementType switch {
            IArrayTypeSymbol childArrayType => GetRootArrayElementType(childArrayType),
            _ => arrayType.ElementType.WithNullableAnnotation(arrayType.ElementNullableAnnotation)
        };

    /// <summary>
    /// Gets the complete set of type arguments for the provided <paramref name="type"/>,
    /// including those provided in containing types.
    /// </summary>
    public static ImmutableList<ITypeSymbol> GetInheritedTypeArguments(INamedTypeSymbol type) {
        return Recurse(type);
        static ImmutableList<ITypeSymbol> Recurse(INamedTypeSymbol type) =>
            type.ContainingType is null
            ? ImmutableList.CreateRange(type.TypeArguments)
            : Recurse(type.ContainingType).AddRange(type.TypeArguments);
    }

    /// <summary>
    /// Gets the complete set of type parameters for the provided <paramref name="type"/>,
    /// including those provided in containing types.
    /// </summary>
    public static ImmutableList<ITypeParameterSymbol> GetInheritedTypeParameters(INamedTypeSymbol type) {
        return Recurse(type);
        static ImmutableList<ITypeParameterSymbol> Recurse(INamedTypeSymbol type) =>
            type.ContainingType is null
            ? ImmutableList.CreateRange(type.TypeParameters)
            : Recurse(type.ContainingType).AddRange(type.TypeParameters);
    }

    /// <summary>
    /// Gets the number of parameters expected by the provided <paramref name="methodSymbol"/>,
    /// including the receiver (this) parameter.
    /// </summary>
    public static int GetParameterCount(IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters.Length + (methodSymbol.ReceiverType is null ? 0 : 1);

    /// <summary>
    /// Gets the type of the parameter to the provided <paramref name="methodSymbol"/> with the
    /// specified <paramref name="index"/>. If the method is not static, then index 0 references
    /// the receiver (this) parameter.
    /// </summary>
    public static ITypeSymbol GetParameterType(IMethodSymbol methodSymbol, int index) {
        if(methodSymbol.ReceiverType is null)
            return methodSymbol.Parameters[index].Type;
        if(index == 0)
            return methodSymbol.ReceiverType;

        return methodSymbol.Parameters[index - 1].Type;
    }

    public static bool IsAccessible(ISymbol symbol) => symbol.DeclaredAccessibility switch {
        Accessibility.Public or Accessibility.Internal or Accessibility.NotApplicable =>
            symbol.ContainingType is null || IsAccessible(symbol.ContainingType),
        _ => false
    };

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
            var typeArgInfo = an.TypeParameters.Select(p => p.Variance)
            .Zip(an.TypeArguments.Zip(bn.TypeArguments));

            return typeArgInfo.All(static tup => tup switch {
                (VarianceKind.In, var (a, b)) => IsSubtype(b, a),
                (VarianceKind.Out, var (a, b)) => IsSubtype(a, b),
                (_, var (a, b)) => SymbolEqualityComparer.Default.Equals(a, b)
            });
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
