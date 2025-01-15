using Microsoft.CodeAnalysis;

namespace Arborist.CodeGen;

internal static class TypeSymbolHelpers {
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

    public static bool TryCreateTypeName(ITypeSymbol type, [NotNullWhen(true)] out string? typeName) {
        var nullAnnotation = NullableAnnotation.Annotated == type.NullableAnnotation && !type.IsValueType ? "?" : "";

        if(type is ITypeParameterSymbol) {
            typeName = string.Concat(type.Name, nullAnnotation);
            return true;
        }

        if(!TryCreateTypeContainingName(type, out var containingName)) {
            typeName = default!;
            return false;
        }

        switch(type) {
            case { IsAnonymousType: true }:
                typeName = default!;
                return false;

            case INamedTypeSymbol { IsGenericType: true } named:
                var typeArgumentNames = new List<string>(named.TypeArguments.Length);
                foreach(var typeArgument in named.TypeArguments) {
                    if(TryCreateTypeName(typeArgument, out var typeArgumentName)) {
                        typeArgumentNames.Add(typeArgumentName);
                    } else {
                        typeName = default!;
                        return false;
                    }
                }

                typeName = string.Concat(
                    containingName,
                    type.Name,
                    typeArgumentNames.MkString("<", ", ", ">"),
                    nullAnnotation
                );
                return true;

            case INamedTypeSymbol named:
                typeName = string.Concat(
                    containingName,
                    type.Name,
                    nullAnnotation
                );
                return true;

            default:
                typeName = default!;
                return false;
        }

    }

    private static bool TryCreateTypeContainingName(
        ITypeSymbol type,
        [NotNullWhen(true)] out string? containingName
    ) {
        if(type.ContainingType is not null) {
            if(TryCreateTypeName(type.ContainingType, out var containingTypeName)) {
                containingName = $"{containingTypeName}.";
                return true;
            } else {
                containingName = default;
                return false;
            }
        }

        containingName = type.ContainingNamespace.IsGlobalNamespace switch {
            true => "global::",
            false => $"{CreateNamespaceName(type.ContainingNamespace)}."
        };
        return true;
    }

    public static string CreateReparametrizedTypeName(
        ITypeSymbol type,
        string prefix,
        ImmutableList<ITypeParameterSymbol> typeParameters,
        bool nullAnnotate = false
    ) {
        // N.B. type parameters have a containing type which can cause infinite recursion
        if(type is ITypeParameterSymbol parameter)
            return string.Concat(
                $"{prefix}{typeParameters.IndexOf(parameter, SymbolEqualityComparer.Default)}",
                NullableAnnotation.Annotated == type.NullableAnnotation && nullAnnotate ? "?" : ""
            );

        var containingName = type switch {
            { ContainingType: not null } => $"{CreateReparametrizedTypeName(type.ContainingType, prefix, typeParameters, true)}.",
            { ContainingNamespace.IsGlobalNamespace: true } => "global::",
            _ => $"{CreateNamespaceName(type.ContainingNamespace)}."
        };

        var nullAnnotation = nullAnnotate switch {
            true when NullableAnnotation.Annotated == type.NullableAnnotation && !type.IsValueType => "?",
            _ => ""
        };

        switch(type) {
            case INamedTypeSymbol { IsGenericType: true } generic:
                return string.Concat(
                    containingName,
                    type.Name,
                    generic.TypeArguments.MkString("<", a => CreateReparametrizedTypeName(a, prefix, typeParameters, nullAnnotate: true), ", ", ">"),
                    nullAnnotation
                );

            case INamedTypeSymbol named:
                return string.Concat(
                    containingName,
                    named.Name,
                    nullAnnotation
                );

            default:
                throw new ArgumentException(nameof(type));
        }
    }

    public static string CreateNamespaceName(INamespaceSymbol ns) =>
        ns.ContainingNamespace.IsGlobalNamespace switch {
            true => $"global::{ns.Name}",
            false => $"{CreateNamespaceName(ns.ContainingNamespace)}.{ns.Name}"
        };

    public static IReadOnlyList<string> GetReparametrizedTypeConstraints(
        string prefix,
        ImmutableList<ITypeParameterSymbol> typeParameters
    ) =>
        new List<string>(
            from typeParameter in typeParameters
            let constraints = GetReparametrizedTypeConstraints(typeParameter, prefix, typeParameters).MkString(", ")
            where constraints.Length != 0
            select $"{CreateReparametrizedTypeName(typeParameter, prefix, typeParameters, nullAnnotate: false)} : {constraints}"
        );

    private static IEnumerable<string> GetReparametrizedTypeConstraints(
        ITypeParameterSymbol typeParameter,
        string prefix,
        ImmutableList<ITypeParameterSymbol> typeParameters
    ) {
        for(var i = 0; i < typeParameter.ConstraintTypes.Length; i++)
            yield return CreateReparametrizedTypeName(
                typeParameter.ConstraintTypes[i].WithNullableAnnotation(typeParameter.ConstraintNullableAnnotations[i]),
                prefix,
                typeParameters,
                nullAnnotate: true
            );
        if(typeParameter.HasNotNullConstraint)
            yield return "notnull";
        if(typeParameter.HasReferenceTypeConstraint)
            yield return "class";
        if(typeParameter.HasUnmanagedTypeConstraint)
            yield return "unmanaged";
        if(typeParameter.HasValueTypeConstraint)
            yield return "struct";
        if(typeParameter.HasConstructorConstraint)
            yield return "new()";
    }

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

    /// <summary>
    /// Returns true if the provided <paramref name="type"/> and all of its constituent types
    /// are named types.
    /// </summary>
    public static bool IsNameableType(ITypeSymbol type) {
        if(type is ITypeParameterSymbol)
            return true;
        if(type is not INamedTypeSymbol named)
            return false;
        if(named.IsAnonymousType)
            return false;
        if(named.IsGenericType && !named.TypeArguments.All(IsNameableType))
            return false;
        if(type.ContainingType is not null && !IsNameableType(type.ContainingType))
            return false;
        return true;
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
