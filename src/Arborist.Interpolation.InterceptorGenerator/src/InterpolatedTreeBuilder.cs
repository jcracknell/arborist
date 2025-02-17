using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

public class InterpolatedTreeBuilder {
    private int _identifierCount;
    private readonly InterpolationDiagnosticsCollector _diagnostics;
    private readonly Dictionary<ITypeSymbol, InterpolatedValueDefinition> _typeRefs;
    private readonly Dictionary<ITypeSymbol, string> _typeRefFactories;
    private readonly List<InterpolatedTree> _methodDefinitions;
    private readonly InterpolatedValueDefinition.Factory _definitionFactory;

    public InterpolatedTreeBuilder(InterpolationDiagnosticsCollector diagnostics) {
        _identifierCount = 0;
        _diagnostics = diagnostics;
        _typeRefs = new(SymbolEqualityComparer.IncludeNullability);
        _typeRefFactories = new(SymbolEqualityComparer.IncludeNullability);
        _methodDefinitions = new();
        _definitionFactory = new InterpolatedValueDefinition.Factory(() => _typeRefs.Values.Count(static d => d.IsInitialized));
    }

    public string DataIdentifier { get; } = "__data";

    public IEnumerable<InterpolatedValueDefinition> ValueDefinitions =>
        _typeRefs.Values.OrderBy(r => r.Order);

    public IEnumerable<InterpolatedTree> MethodDefinitions =>
        _methodDefinitions;

    public string ExpressionTypeName { get; } = "global::System.Linq.Expressions.Expression";

    public string CreateIdentifier() {
        var identifier = $"__v{_identifierCount}";
        _identifierCount += 1;
        return identifier;
    }

    public InterpolatedTree CreateExpression(string factoryName, params InterpolatedTree[] args) =>
        InterpolatedTree.Call(
            InterpolatedTree.Verbatim($"{ExpressionTypeName}.{factoryName}"),
            args
        );

    public InterpolatedTree CreateExpression(string factoryName, IEnumerable<InterpolatedTree> args) =>
        InterpolatedTree.Call(
            InterpolatedTree.Verbatim($"{ExpressionTypeName}.{factoryName}"),
            [..args]
        );

    public InterpolatedTree CreateExpressionArray(IEnumerable<InterpolatedTree> elements) =>
        InterpolatedTree.Concat(
            InterpolatedTree.Verbatim($"new {ExpressionTypeName}[] "),
            InterpolatedTree.Initializer(elements.ToList())
        );


    public InterpolatedTree CreateDefaultValue(ITypeSymbol type) {
        if(TryCreateTypeName(type.WithNullableAnnotation(NullableAnnotation.None), out var typeName))
            return type.IsValueType || NullableAnnotation.Annotated == type.NullableAnnotation
            ? InterpolatedTree.Interpolate($"default({typeName})")
            : InterpolatedTree.Interpolate($"default({typeName})!");

        var typeRef = CreateTypeRef(type);
        return InterpolatedTree.Member(typeRef, InterpolatedTree.Verbatim("Default"));
    }

    public InterpolatedTree CreateCast(ITypeSymbol type, InterpolatedTree tree) {
        if(TryCreateTypeName(type, out var typeName))
            return InterpolatedTree.Interpolate($"({typeName}){tree}");

        var typeRef = CreateTypeRef(type);
        return InterpolatedTree.Interpolate($"{typeRef}.Cast({tree})");
    }

    public InterpolatedTree CreateType(ITypeSymbol type) {
        if(!TypeSymbolHelpers.IsAccessible(type))
            return _diagnostics.InaccessibleSymbol(type, default);

        // If this is a nameable type, then return an inline type reference
        if(TryCreateTypeName(type.WithNullableAnnotation(NullableAnnotation.None), out var typeName))
            return InterpolatedTree.Interpolate($"typeof({typeName})");

        var typeRef = CreateTypeRef(type);
        return InterpolatedTree.Member(typeRef, InterpolatedTree.Verbatim("Type"));
    }

    /// <summary>
    /// Creates an <see cref="InterpolatedTree"/> containing the name of the provided <paramref name="typeSymbol"/>.
    /// Reports a diagnostic message in the event that the <paramref name="typeSymbol"/> cannot be named or referenced.
    /// </summary>
    public InterpolatedTree CreateTypeName(
        ITypeSymbol typeSymbol,
        SyntaxNode? node,
        IReadOnlyDictionary<ITypeParameterSymbol, string>? typeParameterMappings = default
    ) {
        var result = TypeSymbolHelpers.TryGetTypeName(
            typeSymbol: typeSymbol,
            typeParameterMappings: typeParameterMappings ?? ImmutableDictionary<ITypeParameterSymbol, string>.Empty,
            out var typeName
        );

        if(result.IsSuccess)
            return InterpolatedTree.Verbatim(typeName);

        switch(result.Reason) {
            case TypeSymbolHelpers.TypeNameFailureReason.Unhandled:
            case TypeSymbolHelpers.TypeNameFailureReason.AnonymousType:
                return _diagnostics.UnsupportedType(result.TypeSymbol, node);

            case TypeSymbolHelpers.TypeNameFailureReason.TypeParameter:
                // A type parameter symbol must be from the call site, as declaration site type parameters are
                // obviously only in scope within the declaration.
                return _diagnostics.ReferencesCallSiteTypeParameter(result.TypeSymbol, node);

            case TypeSymbolHelpers.TypeNameFailureReason.Inaccessible:
                return _diagnostics.InaccessibleSymbol(result.TypeSymbol, node);

            default:
                throw new NotImplementedException(result.Reason.ToString());
        }
    }

    /// <summary>
    /// Attempts to create an <see cref="InterpolatedTree"/> containing the name of the provided
    /// <paramref name="typeSymbol"/>. Reports no diagnostic messages.
    /// </summary>
    public bool TryCreateTypeName(
        ITypeSymbol typeSymbol,
        [NotNullWhen(true)] out InterpolatedTree? typeName,
        IReadOnlyDictionary<ITypeParameterSymbol, string>? typeParameterMappings = default
    ) {
        var result = TypeSymbolHelpers.TryGetTypeName(
            typeSymbol: typeSymbol,
            typeParameterMappings: typeParameterMappings ?? ImmutableDictionary<ITypeParameterSymbol, string>.Empty,
            out var typeNameString
        );

        if(result.IsSuccess) {
            typeName = InterpolatedTree.Verbatim(typeNameString);
            return true;
        } else {
            typeName = default;
            return false;
        }
    }

    public InterpolatedTree CreateTypeRef(ITypeSymbol type) {
        if(!TypeSymbolHelpers.IsAccessible(type))
            return _diagnostics.InaccessibleSymbol(type, default);

        if(_typeRefs.TryGetValue(type, out var cached)) {
            // This shouldn't be possible, as it would require a self-referential generic type
            if(!cached.IsInitialized)
                return _diagnostics.UnsupportedType(type, default);

            return InterpolatedTree.Verbatim(cached.Identifier);
        }

        var definition = _definitionFactory.Create($"__t{_typeRefs.Count}");
        _typeRefs[type] = definition;

        _definitionFactory.Set(definition, CreateTypeRefUncached(type));
        return InterpolatedTree.Verbatim(definition.Identifier);
    }

    private InterpolatedTree CreateTypeRefUncached(ITypeSymbol type) {
        switch(type) {
            case ITypeParameterSymbol:
                // A type parameter symbol must be from the call site, as declaration site type parameters are
                // obviously only in scope within the declaration.
                return _diagnostics.ReferencesCallSiteTypeParameter(type, default);

            case { IsAnonymousType: true }:
                return InterpolatedTree.Call(
                    InterpolatedTree.Verbatim("global::Arborist.Interpolation.Internal.TypeRef.Create"),
                    [InterpolatedTree.Concat(
                        InterpolatedTree.Verbatim("new "),
                        InterpolatedTree.Initializer([..(
                            from property in type.GetMembers().OfType<IPropertySymbol>()
                            select InterpolatedTree.Interpolate($"{property.Name} = {CreateDefaultValue(property.Type)}")
                        )])
                    )]
                );

            case INamedTypeSymbol named when TryCreateTypeName(named, out var typeName):
                return InterpolatedTree.Interpolate($"global::Arborist.Interpolation.Internal.TypeRef<{typeName}>.Instance");

            // If we have a generic type containing an anonymous type, we can generate a static local
            // function to construct the required typeref from other typeref instances
            case INamedTypeSymbol { IsGenericType: true } named:
                return CreateTypeRefFactory(named);

            default:
                return _diagnostics.UnsupportedType(type, default);
        }
    }

    private InterpolatedTree CreateTypeRefFactory(INamedTypeSymbol type) {
        var constructedFrom = (INamedTypeSymbol)type.ConstructedFrom.WithNullableAnnotation(type.NullableAnnotation);
        if(!_typeRefFactories.TryGetValue(constructedFrom, out var methodName)) {
            methodName = $"TypeRefFactory{_typeRefFactories.Count}";
            _typeRefFactories[constructedFrom] = methodName;

            var typeParameters = TypeSymbolHelpers.GetInheritedTypeParameters(constructedFrom);
            var typeParameterMappings = typeParameters.ZipWithIndex().ToDictionary(
                tup => (ITypeParameterSymbol)tup.Value.WithNullableAnnotation(NullableAnnotation.None),
                tup => $"TR{tup.Index}",
                (IEqualityComparer<ITypeParameterSymbol>)SymbolEqualityComparer.Default
            );

            var typeArguments = Enumerable.Range(0, typeParameters.Count).MkString("<", i => $"TR{i}", ", ", ">");
            var reparametrizedTypeName = CreateTypeName(constructedFrom, default, typeParameterMappings);

            _methodDefinitions.Add(InterpolatedTree.MethodDefinition(
                InterpolatedTree.Interpolate($"static global::Arborist.Interpolation.Internal.TypeRef<{reparametrizedTypeName}> {methodName}{typeArguments}"),
                [..(
                    from i in Enumerable.Range(0, typeParameters.Count)
                    let typeParameter = typeParameters[i]
                    select InterpolatedTree.Verbatim($"global::Arborist.Interpolation.Internal.TypeRef<TR{i}> tr{i}")
                )],
                GetReparametrizedTypeConstraints(typeParameters, typeParameterMappings),
                InterpolatedTree.ArrowBody(InterpolatedTree.Verbatim("default!"))
            ));
        }

        return InterpolatedTree.Call(
            InterpolatedTree.Verbatim(methodName),
            [..TypeSymbolHelpers.GetInheritedTypeArguments(type).Select(CreateTypeRef)]
        );
    }

    public IReadOnlyList<InterpolatedTree> GetReparametrizedTypeConstraints(
        ImmutableList<ITypeParameterSymbol> typeParameters,
        IReadOnlyDictionary<ITypeParameterSymbol, string> typeParameterMappings
    ) {
        var results = new List<InterpolatedTree>(0);
        foreach(var typeParameter in typeParameters) {
            var constraints = GetReparametrizedTypeConstraintConstraints(typeParameter, typeParameterMappings);
            if(constraints.Count != 0)
                results.Add(InterpolatedTree.Interpolate($"{typeParameter.Name} : {InterpolatedTree.Concat(constraints)}"));
        }

        return results;
    }

    private IReadOnlyList<InterpolatedTree> GetReparametrizedTypeConstraintConstraints(
        ITypeParameterSymbol typeParameter,
        IReadOnlyDictionary<ITypeParameterSymbol, string> typeParameterMappings
    ) {
        var constraints = new List<InterpolatedTree>(typeParameter.ConstraintTypes.Length);

        for(var i = 0; i < typeParameter.ConstraintTypes.Length; i++)
            constraints.Add(CreateTypeName(typeParameter.ConstraintTypes[i], null, typeParameterMappings));
        if(typeParameter.HasNotNullConstraint)
            constraints.Add(InterpolatedTree.Verbatim("notnull"));
        if(typeParameter.HasReferenceTypeConstraint)
            constraints.Add(InterpolatedTree.Verbatim("class"));
        if(typeParameter.HasUnmanagedTypeConstraint)
            constraints.Add(InterpolatedTree.Verbatim("unmanaged"));
        if(typeParameter.HasValueTypeConstraint)
            constraints.Add(InterpolatedTree.Verbatim("struct"));
        if(typeParameter.HasConstructorConstraint)
            constraints.Add(InterpolatedTree.Verbatim("new()"));

        return constraints;
    }
}
