using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public class InterpolatedExpressionBuilder {
    private int _identifierCount;
    private readonly DiagnosticFactory _diagnostics;
    private readonly Dictionary<IMethodSymbol, LocalDefinition> _methodInfos;
    private readonly Dictionary<(string, ITypeSymbol), LocalDefinition> _parameters;
    private readonly Dictionary<ITypeSymbol, LocalDefinition> _typeRefs;
    private readonly Dictionary<ITypeSymbol, string> _typeRefFactories;
    private readonly IReadOnlyList<IReadOnlyCollection<LocalDefinition>> _valueDefinitionCollections;
    private readonly List<InterpolatedTree> _methodDefinitions;
    private readonly LocalDefinition.Factory _definitionFactory;

    public InterpolatedExpressionBuilder(DiagnosticFactory diagnostics) {
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


        _definitionFactory = new LocalDefinition.Factory(GetValueDefinitionCount);
    }

    public string DataIdentifier { get; } = "__data";

    protected int GetValueDefinitionCount() =>
        _valueDefinitionCollections.Sum(static c => c.Count(static d => d.IsInitialized));

    public IEnumerable<LocalDefinition> ValueDefinitions =>
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
            ..parameters
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

    public InterpolatedTree CreateExpressionType(SyntaxNode syntax) {
        if(TryGetExpressionTypeName(syntax) is not {} name)
            return _diagnostics.UnsupportedInterpolatedSyntax(syntax);

        return InterpolatedTree.Verbatim($"global::System.Linq.Expressions.ExpressionType.{name}");
    }

    private string? TryGetExpressionTypeName(SyntaxNode syntax) => syntax.Kind() switch {
        SyntaxKind.CoalesceExpression => nameof(ExpressionType.Coalesce),
        // Logic
        SyntaxKind.LogicalNotExpression => nameof(ExpressionType.Not),
        SyntaxKind.LogicalAndExpression => nameof(ExpressionType.AndAlso),
        SyntaxKind.LogicalOrExpression => nameof(ExpressionType.OrElse),
        // Comparison
        SyntaxKind.EqualsExpression => nameof(ExpressionType.Equal),
        SyntaxKind.NotEqualsExpression => nameof(ExpressionType.NotEqual),
        SyntaxKind.LessThanExpression => nameof(ExpressionType.LessThan),
        SyntaxKind.LessThanOrEqualExpression => nameof(ExpressionType.LessThanOrEqual),
        SyntaxKind.GreaterThanExpression => nameof(ExpressionType.GreaterThan),
        SyntaxKind.GreaterThanOrEqualExpression => nameof(ExpressionType.GreaterThanOrEqual),
        // Arithmetic
        SyntaxKind.UnaryMinusExpression => nameof(ExpressionType.Negate),
        SyntaxKind.UnaryPlusExpression => nameof(ExpressionType.UnaryPlus),
        SyntaxKind.AddExpression => nameof(ExpressionType.Add),
        SyntaxKind.AddAssignmentExpression => nameof(ExpressionType.AddAssign),
        SyntaxKind.SubtractExpression => nameof(ExpressionType.Subtract),
        SyntaxKind.SubtractAssignmentExpression => nameof(ExpressionType.SubtractAssign),
        SyntaxKind.MultiplyExpression => nameof(ExpressionType.Multiply),
        SyntaxKind.MultiplyAssignmentExpression => nameof(ExpressionType.MultiplyAssign),
        SyntaxKind.DivideExpression => nameof(ExpressionType.Divide),
        SyntaxKind.DivideAssignmentExpression => nameof(ExpressionType.DivideAssign),
        SyntaxKind.ModuloExpression => nameof(ExpressionType.Modulo),
        SyntaxKind.ModuloAssignmentExpression => nameof(ExpressionType.ModuloAssign),
        // Bitwise
        SyntaxKind.BitwiseNotExpression => nameof(ExpressionType.Not),
        SyntaxKind.BitwiseAndExpression => nameof(ExpressionType.And),
        SyntaxKind.BitwiseOrExpression => nameof(ExpressionType.Or),
        SyntaxKind.ExclusiveOrExpression => nameof(ExpressionType.ExclusiveOr),
        SyntaxKind.ExclusiveOrAssignmentExpression => nameof(ExpressionType.ExclusiveOrAssign),
        SyntaxKind.LeftShiftExpression => nameof(ExpressionType.LeftShift),
        SyntaxKind.LeftShiftAssignmentExpression => nameof(ExpressionType.LeftShiftAssign),
        SyntaxKind.RightShiftExpression => nameof(ExpressionType.RightShift),
        SyntaxKind.RightShiftAssignmentExpression => nameof(ExpressionType.RightShiftAssign),
        _ => default
    };

    public InterpolatedTree CreateDefaultValue(ITypeSymbol type) {
        if(TypeSymbolHelpers.TryCreateTypeName(type.WithNullableAnnotation(NullableAnnotation.None), out var typeName))
            return type.IsValueType || NullableAnnotation.Annotated == type.NullableAnnotation
            ? InterpolatedTree.Verbatim($"default({typeName})")
            : InterpolatedTree.Verbatim($"default({typeName})!");

        var typeRef = CreateTypeRef(type);
        return InterpolatedTree.Member(typeRef, InterpolatedTree.Verbatim("Default"));
    }

    public InterpolatedTree CreateType(ITypeSymbol type) {
        if(!TypeSymbolHelpers.IsAccessible(type))
            return _diagnostics.InaccesibleSymbol(type);

        // If this is a nameable type, then return an inline type reference
        if(TypeSymbolHelpers.TryCreateTypeName(type, out var typeName))
            return InterpolatedTree.Verbatim($"typeof({typeName})");

        var typeRef = CreateTypeRef(type);
        return InterpolatedTree.Member(typeRef, InterpolatedTree.Verbatim("Type"));
    }

    public InterpolatedTree CreateTypeRef(ITypeSymbol type) {
        if(!TypeSymbolHelpers.IsAccessible(type))
            return _diagnostics.InaccesibleSymbol(type);

        if(_typeRefs.TryGetValue(type, out var cached)) {
            // This shouldn't be possible, as it would require a self-referential generic type
            if(!cached.IsInitialized)
                return _diagnostics.UnsupportedType(type);

            return InterpolatedTree.Verbatim(cached.Identifier);
        }

        var definition = _definitionFactory.Create($"__t{_typeRefs.Count}");
        _typeRefs[type] = definition;
        try {
            definition.SetInitializer(CreateTypeRefUncached(type));
            return InterpolatedTree.Verbatim(definition.Identifier);
        } catch {
            _typeRefs.Remove(type);
            throw;
        }
    }

    private InterpolatedTree CreateTypeRefUncached(ITypeSymbol type) {
        switch(type) {
            case { IsAnonymousType: true }:
                return InterpolatedTree.StaticCall(
                    InterpolatedTree.Verbatim("global::Arborist.Interpolation.Internal.TypeRef.Create"),
                    [InterpolatedTree.Concat(
                        InterpolatedTree.Verbatim("new "),
                        InterpolatedTree.Initializer([..(
                            from property in type.GetMembers().OfType<IPropertySymbol>()
                            select InterpolatedTree.Concat(
                                InterpolatedTree.Verbatim($"{property.Name} = "),
                                CreateDefaultValue(property.Type)
                            )
                        )])
                    )]
                );

            case INamedTypeSymbol named when TypeSymbolHelpers.TryCreateTypeName(named, out var typeName):
                return InterpolatedTree.Verbatim(
                    $"global::Arborist.Interpolation.Internal.TypeRef<{typeName}>.Instance"
                );

            // If we have a generic type containing an anonymous type, we can generate a static local
            // function to construct the required typeref from other typeref instances
            case INamedTypeSymbol { IsGenericType: true } named:
                return CreateTypeRefFactory(named);

            default:
                return _diagnostics.UnsupportedType(type);
        }
    }

    private InterpolatedTree CreateTypeRefFactory(INamedTypeSymbol type) {
        var constructedFrom = type.ConstructedFrom.WithNullableAnnotation(type.NullableAnnotation);
        if(!_typeRefFactories.TryGetValue(constructedFrom, out var methodName)) {
            methodName = $"TypeRefFactory{_typeRefFactories.Count}";
            _typeRefFactories[constructedFrom] = methodName;

            var typeParameters = TypeSymbolHelpers.GetInheritedTypeParameters(type);
            var reparametrizedTypeName = TypeSymbolHelpers.CreateReparametrizedTypeName(constructedFrom, typeParameters, nullAnnotate: true);
            var typeArguments = Enumerable.Range(0, typeParameters.Count).MkString("<", i => $"T{i}", ", ", ">");

            _methodDefinitions.Add(InterpolatedTree.MethodDefinition(
                $"static global::Arborist.Internal.TypeRef<{reparametrizedTypeName}> {methodName}{typeArguments}",
                [..(
                    from i in Enumerable.Range(0, typeParameters.Count)
                    let typeParameter = typeParameters[i]
                    select InterpolatedTree.Verbatim($"global::Arborist.Interpolation.Internal.TypeRef<T{i}> t{i}")
                )],
                [..(
                    from constraint in TypeSymbolHelpers.GetReparametrizedTypeConstraints(typeParameters)
                    select InterpolatedTree.Verbatim(constraint)
                )],
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

    public InterpolatedTree CreateMethodInfo(IMethodSymbol method, InvocationExpressionSyntax? node) {
        if(_methodInfos.TryGetValue(method, out var cached))
            return InterpolatedTree.Verbatim(cached.Identifier);

        var definition = _definitionFactory.Create($"__m{_methodInfos.Count}");
        _methodInfos[method] = definition;
        try {
            definition.SetInitializer(CreateMethodInfoUncached(method, node));
            return InterpolatedTree.Verbatim(definition.Identifier);
        } catch {
            _methodInfos.Remove(method);
            throw;
        }
    }

    private InterpolatedTree CreateMethodInfoUncached(IMethodSymbol method, InvocationExpressionSyntax? node) {
        if(method.IsGenericMethod)
            return InterpolatedTree.StaticCall(
                InterpolatedTree.Verbatim("global::Arborist.ExpressionOnNone.GetMethodInfo"),
                [InterpolatedTree.Lambda([], CreateGenericMethodCall(method, node))]
            );

        var declaringType = CreateType(method.ContainingType);
        var parameterTypes = CreateTypeArray(method.Parameters.Select(p => p.Type));

        return InterpolatedTree.Concat(
            InterpolatedTree.InstanceCall(
                declaringType,
                InterpolatedTree.Verbatim("GetMethod"), [
                InterpolatedTree.Verbatim($"\"{method.Name}\""),
                parameterTypes
            ]),
            InterpolatedTree.Verbatim("!")
        );
    }

    private InterpolatedTree CreateGenericMethodCall(IMethodSymbol method, InvocationExpressionSyntax? node) {
        // Roslyn does a stupid thing where it represents extension methods as instance
        // methods, which makes dealing with them a pain in the ass.
        if(method.ReducedFrom is not null)
            return CreateGenericMethodCall(method.ReducedFrom.Construct(method.TypeArguments, method.TypeArgumentNullableAnnotations), node);

        var typeArgs = CreateGenericMethodCallTypeArgs(method, node);
        var valueArgs = method.Parameters.Select(p => CreateDefaultValue(p.Type)).ToList();

        if(method.IsStatic) {
            if(!TypeSymbolHelpers.TryCreateTypeName(method.ContainingType, out var containingTypeName))
                return _diagnostics.UnsupportedType(method.ContainingType);

            return InterpolatedTree.StaticCall(
                InterpolatedTree.Concat(
                    InterpolatedTree.Verbatim($"{containingTypeName}.{method.Name}"),
                    typeArgs
                ),
                valueArgs
            );
        } else {
            return InterpolatedTree.InstanceCall(
                CreateDefaultValue(method.ContainingType),
                InterpolatedTree.Concat(
                    InterpolatedTree.Verbatim(method.Name),
                    typeArgs
                ),
                valueArgs
            );
        }
    }

    private InterpolatedTree CreateGenericMethodCallTypeArgs(IMethodSymbol methodSymbol, InvocationExpressionSyntax? node) {
        switch(node) {
            // Only specify type arguments if they were explicitly specified in the original call
            // (in which case we know they are nameable).
            case { Expression: MemberAccessExpressionSyntax { Name: GenericNameSyntax } }:
                var typeArgNames = new List<string>(methodSymbol.TypeArguments.Length);
                foreach(var typeArg in methodSymbol.TypeArguments) {
                    if(TypeSymbolHelpers.TryCreateTypeName(typeArg, out var typeArgName)) {
                        typeArgNames.Add(typeArgName);
                    } else {
                        return _diagnostics.UnsupportedType(typeArg);
                    }
                }

                return InterpolatedTree.Verbatim(typeArgNames.MkString("<", ", ", ">"));

            default:
                return InterpolatedTree.Empty;
        }
    }

    public InterpolatedTree CreateParameter(ITypeSymbol type, string name) {
        var cacheKey = (name, type);
        if(_parameters.TryGetValue(cacheKey, out var cached))
            return InterpolatedTree.Verbatim(cached.Identifier);

        var definition = _definitionFactory.Create($"__p{_parameters.Count}");
        _parameters[cacheKey] = definition;
        try {
            var parameterExpression = CreateExpression(nameof(Expression.Parameter), [
                CreateType(type),
                InterpolatedTree.Verbatim($"\"{name}\"")
            ]);

            definition.SetInitializer(parameterExpression);
            return InterpolatedTree.Verbatim(definition.Identifier);
        } catch {
            _parameters.Remove(cacheKey);
            throw;
        }
    }

    private sealed class ParameterDefinitionsKeyEqualityComparer : IEqualityComparer<(string, ITypeSymbol)> {
        public static ParameterDefinitionsKeyEqualityComparer Instance { get; } = new();

        private ParameterDefinitionsKeyEqualityComparer() { }

        public bool Equals((string, ITypeSymbol) a, (string, ITypeSymbol) b) =>
            StringComparer.Ordinal.Equals(a.Item1, b.Item1)
            && SymbolEqualityComparer.Default.Equals(a.Item2, b.Item2);

        public int GetHashCode((string, ITypeSymbol) obj) =>
            StringComparer.Ordinal.GetHashCode(obj.Item1)
            ^ SymbolEqualityComparer.Default.GetHashCode(obj.Item2);
    }
}
