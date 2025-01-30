using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedTreeBuilder {
    private int _identifierCount;
    private readonly InterpolationDiagnosticsCollector _diagnostics;
    private readonly Dictionary<IMethodSymbol, InterpolatedValueDefinition> _methodInfos;
    private readonly Dictionary<(string, ITypeSymbol), InterpolatedValueDefinition> _parameters;
    private readonly Dictionary<ITypeSymbol, InterpolatedValueDefinition> _typeRefs;
    private readonly Dictionary<ITypeSymbol, string> _typeRefFactories;
    private readonly IReadOnlyList<IReadOnlyCollection<InterpolatedValueDefinition>> _valueDefinitionCollections;
    private readonly List<InterpolatedTree> _methodDefinitions;
    private readonly InterpolatedValueDefinition.Factory _definitionFactory;

    public InterpolatedTreeBuilder(InterpolationDiagnosticsCollector diagnostics) {
        _identifierCount = 0;
        _diagnostics = diagnostics;
        _methodInfos = new(SymbolEqualityComparer.Default);
        _parameters = new(ParameterDefinitionsKeyEqualityComparer.Instance);
        _typeRefs = new(SymbolEqualityComparer.IncludeNullability);
        _typeRefFactories = new(SymbolEqualityComparer.IncludeNullability);
        _methodDefinitions = new();

        _valueDefinitionCollections = [
            _methodInfos.Values,
            _parameters.Values,
            _typeRefs.Values
        ];


        _definitionFactory = new InterpolatedValueDefinition.Factory(GetValueDefinitionCount);
    }

    public string DataIdentifier { get; } = "__data";

    protected int GetValueDefinitionCount() =>
        _valueDefinitionCollections.Sum(static c => c.Count(static d => d.IsInitialized));

    public IEnumerable<InterpolatedValueDefinition> ValueDefinitions =>
        _valueDefinitionCollections.SelectMany(static x => x)
        .OrderBy(static r => r.Order);

    public IEnumerable<InterpolatedTree> MethodDefinitions =>
        _methodDefinitions;

    public string ExpressionTypeName { get; } = "global::System.Linq.Expressions.Expression";

    public string CreateIdentifier() {
        var identifier = $"__v{_identifierCount}";
        _identifierCount += 1;
        return identifier;
    }

    public InterpolatedTree CreateAnonymousClassExpression(ITypeSymbol type, IReadOnlyList<InterpolatedTree> parameters) =>
        CreateExpression(nameof(Expression.New), [
            InterpolatedTree.Indexer(
                InterpolatedTree.InstanceCall(
                    CreateType(type),
                    InterpolatedTree.Verbatim("GetConstructors"),
                    []
                ),
                InterpolatedTree.Verbatim("0")
            ),
            CreateExpressionArray(parameters),
            // Bind the properties of the anonymous type in the order they are declared
            InterpolatedTree.Concat(
                InterpolatedTree.Verbatim("new global::System.Reflection.MemberInfo[] "),
                InterpolatedTree.Initializer([..(
                    from property in type.GetMembers().OfType<IPropertySymbol>()
                    select InterpolatedTree.Concat(
                        CreateType(type),
                        InterpolatedTree.Verbatim($".GetProperty(\"{property.Name}\")!")
                    )
                )])
            )
        ]);

    public InterpolatedTree CreateExpression(string factoryName, params InterpolatedTree[] args) =>
        InterpolatedTree.StaticCall(
            InterpolatedTree.Verbatim($"{ExpressionTypeName}.{factoryName}"),
            args
        );

    public InterpolatedTree CreateExpression(string factoryName, IEnumerable<InterpolatedTree> args) =>
        InterpolatedTree.StaticCall(
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
                return InterpolatedTree.StaticCall(
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

        return InterpolatedTree.StaticCall(
            InterpolatedTree.Verbatim(methodName),
            [..TypeSymbolHelpers.GetInheritedTypeArguments(type).Select(CreateTypeRef)]
        );
    }
    

    public InterpolatedTree CreateTypeArray(IEnumerable<ITypeSymbol> types) =>
        types switch {
            IReadOnlyCollection<ITypeSymbol> { Count: 0 } => InterpolatedTree.Verbatim("global::System.Type.EmptyTypes"),
            _ => InterpolatedTree.Concat(
                InterpolatedTree.Verbatim("new global::System.Type[] "),
                InterpolatedTree.Initializer(types.Select(CreateType).ToList())
            )
        };

    public InterpolatedTree CreateMethodInfo(
        IMethodSymbol methodSymbol,
        SyntaxNode? node,
        bool requireTypeParameters = false
    ) {
        // The requireTypeParameters flag feels like a hack, however in the VAST majority of cases you don't
        // want them, as you are more likely to be able to resolve the generic method. This is intended to be
        // used for calls to Enumerable.Cast<T> originating from LINQ queries, which are implicit but MUST
        // specify type parameters.
        if(_methodInfos.TryGetValue(methodSymbol, out var cached))
            return InterpolatedTree.Verbatim(cached.Identifier);

        var definition = _definitionFactory.Create($"__m{_methodInfos.Count}");
        _methodInfos[methodSymbol] = definition;

        _definitionFactory.Set(definition, CreateMethodInfoUncached(methodSymbol, node, requireTypeParameters));
        return InterpolatedTree.Verbatim(definition.Identifier);
    }

    private InterpolatedTree CreateMethodInfoUncached(
        IMethodSymbol methodSymbol,
        SyntaxNode? node,
        bool requireTypeParameters
    ) {
        // It FEELS like this should be optimizable, however in practice it seems very difficult to improve on this,
        // as without HEAVY caching the cost of resolving the method appears to be approximately equivalent to the
        // cost of constructing the expression, which already contains the pre-resolved and constructed MethodInfo.
        if(methodSymbol.IsGenericMethod && methodSymbol.MethodKind is not (MethodKind.BuiltinOperator or MethodKind.UserDefinedOperator))
            return InterpolatedTree.StaticCall(
                InterpolatedTree.Verbatim("global::Arborist.ExpressionOnNone.GetMethodInfo"),
                [InterpolatedTree.Lambda([], CreateGenericMethodCall(methodSymbol, node, requireTypeParameters))]
            );

        var declaringType = CreateType(methodSymbol.ContainingType);
        var parameterTypes = CreateTypeArray(methodSymbol.Parameters.Select(p => p.Type));

        return InterpolatedTree.Concat(
            InterpolatedTree.InstanceCall(
                declaringType,
                InterpolatedTree.Verbatim("GetMethod"), [
                InterpolatedTree.Verbatim($"\"{methodSymbol.Name}\""),
                parameterTypes
            ]),
            InterpolatedTree.Verbatim("!")
        );
    }

    private InterpolatedTree CreateGenericMethodCall(
        IMethodSymbol methodSymbol,
        SyntaxNode? node,
        bool requireTypeParameters
    ) {
        // Roslyn does a stupid thing where it represents extension methods as instance
        // methods, which makes dealing with them a pain in the ass.
        if(methodSymbol.ReducedFrom is not null)
            return CreateGenericMethodCall(
                methodSymbol.ReducedFrom.Construct(methodSymbol.TypeArguments, methodSymbol.TypeArgumentNullableAnnotations),
                node,
                requireTypeParameters
            );

        var typeArgs = CreateGenericMethodCallTypeArgs(methodSymbol, node, requireTypeParameters);
        
        var valueArgs = new InterpolatedTree[methodSymbol.Parameters.Length];
        for(var i = 0; i < methodSymbol.Parameters.Length; i++)
            valueArgs[i] = CreateDefaultValue(methodSymbol.Parameters[i].Type);

        if(methodSymbol.IsStatic) {
            var containingTypeName = CreateTypeName(methodSymbol.ContainingType, node);
            
            return InterpolatedTree.StaticCall(
                InterpolatedTree.Interpolate($"{containingTypeName}.{methodSymbol.Name}{typeArgs}"),
                valueArgs
            );
        } else {
            return InterpolatedTree.InstanceCall(
                CreateDefaultValue(methodSymbol.ContainingType),
                InterpolatedTree.Interpolate($"{methodSymbol.Name}{typeArgs}"),
                valueArgs
            );
        }
    }

    private InterpolatedTree CreateGenericMethodCallTypeArgs(
        IMethodSymbol methodSymbol,
        SyntaxNode? node,
        bool requireTypeParameters
    ) {
        // Type arguments were not specified in the original invocation - if the node is not an invocation,
        // the invocation is assumed to have been implicit.
        if(!requireTypeParameters && !SyntaxHelpers.IsExplicitGenericMethodInvocation(node))
            return InterpolatedTree.Empty;
        
        var parts = new List<InterpolatedTree>(2 * methodSymbol.TypeArguments.Length + 1);
        parts.Add(InterpolatedTree.Verbatim("<"));
        
        for(var i = 0; i < methodSymbol.TypeArguments.Length; i++) {
            if(i != 0)
                parts.Add(InterpolatedTree.Verbatim(", "));

            parts.Add(CreateTypeName(methodSymbol.TypeArguments[i], node));
        }
        
        parts.Add(InterpolatedTree.Verbatim(">"));
        return InterpolatedTree.Concat(parts);
    }

    public InterpolatedTree CreateParameter(ITypeSymbol type, string name) {
        var cacheKey = (name, type);
        if(_parameters.TryGetValue(cacheKey, out var cached))
            return InterpolatedTree.Verbatim(cached.Identifier);

        var definition = _definitionFactory.Create($"__p{_parameters.Count}");
        _parameters[cacheKey] = definition;

        var parameterExpression = CreateExpression(nameof(Expression.Parameter), [
            CreateType(type),
            InterpolatedTree.Verbatim($"\"{name}\"")
        ]);

        _definitionFactory.Set(definition, parameterExpression);
        return InterpolatedTree.Verbatim(definition.Identifier);
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
    
    private sealed class ParameterDefinitionsKeyEqualityComparer : IEqualityComparer<(string, ITypeSymbol)> {
        public static ParameterDefinitionsKeyEqualityComparer Instance { get; } = new();

        private ParameterDefinitionsKeyEqualityComparer() { }

        public bool Equals((string, ITypeSymbol) a, (string, ITypeSymbol) b) =>
            StringComparer.Ordinal.Equals(a.Item1, b.Item1)
            && SymbolEqualityComparer.Default.Equals(a.Item2, b.Item2);

        public int GetHashCode((string, ITypeSymbol) obj) {
            var hash = new HashCode();
            hash.Add(obj.Item1);
            hash.Add(obj.Item2, SymbolEqualityComparer.Default);
            return hash.ToHashCode();
        }
    }
}
