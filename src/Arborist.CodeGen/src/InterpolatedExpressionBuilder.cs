using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public class InterpolatedExpressionBuilder {
    private const string Unsupported = "???";

    private int _identifierCount;
    private readonly DiagnosticFactory _diagnostics;
    private readonly Dictionary<IMethodSymbol, LocalDefinition> _methodInfos;
    private readonly Dictionary<(string, ITypeSymbol), LocalDefinition> _parameters;
    private readonly Dictionary<ITypeSymbol, LocalDefinition> _typeRefs;
    private readonly IReadOnlyList<IReadOnlyCollection<LocalDefinition>> _valueDefinitionCollections;
    private readonly List<InterpolatedExpressionTree> _methodDefinitions;
    private readonly LocalDefinition.Factory _definitionFactory;

    public InterpolatedExpressionBuilder(DiagnosticFactory diagnostics) {
        _identifierCount = 0;
        _diagnostics = diagnostics;
        _methodInfos = new(SymbolEqualityComparer.Default);
        _parameters = new(ParameterDefinitionsKeyEqualityComparer.Instance);
        _typeRefs = new(SymbolEqualityComparer.IncludeNullability);
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

    public IEnumerable<InterpolatedExpressionTree> MethodDefinitions =>
        _methodDefinitions;

    private A UnsupportedSymbol<A>(ISymbol symbol, A result) =>
        result;

    public string ExpressionTypeName { get; } = "global::System.Linq.Expressions.Expression";

    public string CreateIdentifier() {
        var identifier = $"__v{_identifierCount}";
        _identifierCount += 1;
        return identifier;
    }

    public InterpolatedExpressionTree CreateExpression(string factoryName, params InterpolatedExpressionTree[] args) =>
        InterpolatedExpressionTree.StaticCall($"{ExpressionTypeName}.{factoryName}", args);

    public InterpolatedExpressionTree CreateExpression(string factoryName, IEnumerable<InterpolatedExpressionTree> args) =>
        InterpolatedExpressionTree.StaticCall($"{ExpressionTypeName}.{factoryName}", args.ToList()!);

    public InterpolatedExpressionTree CreateExpressionArray(IEnumerable<InterpolatedExpressionTree> elements) =>
        InterpolatedExpressionTree.Concat(
            InterpolatedExpressionTree.Verbatim($"new {ExpressionTypeName}[] "),
            InterpolatedExpressionTree.Initializer(elements.ToList())
        );

    public InterpolatedExpressionTree CreateExpressionType(SyntaxNode syntax) =>
        InterpolatedExpressionTree.Verbatim($"global::System.Linq.Expressions.ExpressionType.{CreateExpressionTypeName(syntax)}");

    private string CreateExpressionTypeName(SyntaxNode syntax) => syntax.Kind() switch {
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
        _ => _diagnostics.UnsupportedInterpolatedSyntax(syntax, Unsupported)
    };

    public InterpolatedExpressionTree CreateDefaultValue(ITypeSymbol type) {
        var typeRef = CreateTypeRef(type);
        return InterpolatedExpressionTree.Member(typeRef, "Default");
    }

    public InterpolatedExpressionTree CreateType(ITypeSymbol type) {
        if(!TypeSymbolHelpers.IsAccessible(type))
            return _diagnostics.InaccesibleSymbol(type, InterpolatedExpressionTree.Unsupported);

        // If this is a static type, it is not possible to create a TypeRef (as it can't be
        // used as a type parameter)
        if(type.IsStatic && TypeSymbolHelpers.TryCreateTypeName(type, out var typeName))
            return InterpolatedExpressionTree.Verbatim($"typeof({typeName})");

        var typeRef = CreateTypeRef(type);
        return InterpolatedExpressionTree.Member(typeRef, "Type");
    }

    public InterpolatedExpressionTree CreateTypeRef(ITypeSymbol type) {
        if(!TypeSymbolHelpers.IsAccessible(type))
            return _diagnostics.InaccesibleSymbol(type, InterpolatedExpressionTree.Unsupported);

        if(_typeRefs.TryGetValue(type, out var cached)) {
            // This shouldn't be possible, as it would require a self-referential generic type
            if(!cached.IsInitialized)
                return UnsupportedSymbol(type, InterpolatedExpressionTree.Unsupported);

            return InterpolatedExpressionTree.Verbatim(cached.Identifier);
        }

        var definition = _definitionFactory.Create($"__t{_typeRefs.Count}");
        _typeRefs[type] = definition;
        try {
            definition.SetInitializer(CreateTypeRefUncached(type));
            return InterpolatedExpressionTree.Verbatim(definition.Identifier);
        } catch {
            _typeRefs.Remove(type);
            throw;
        }
    }

    private InterpolatedExpressionTree CreateTypeRefUncached(ITypeSymbol type) {
        switch(type) {
            case { IsAnonymousType: true }:
                var anonymousProperties = type.GetMembers().OfType<IPropertySymbol>()
                .MkString(p => $"{p.Name} = {CreateDefaultValue(p.Type)}", ", ");

                return InterpolatedExpressionTree.StaticCall(
                    "global::Arborist.Interpolation.Internal.TypeRef.Create",
                    [InterpolatedExpressionTree.Verbatim($"new {{ {anonymousProperties} }}")]
                );

            case INamedTypeSymbol named when TypeSymbolHelpers.TryCreateTypeName(named, out var typeName):
                return InterpolatedExpressionTree.Verbatim(
                    $"global::Arborist.Interpolation.Internal.TypeRef<{typeName}>.Instance"
                );

            // If we have a generic type containing an anonymous type, we can generate a static local
            // function to construct the required typeref from other typeref instances
            case INamedTypeSymbol { IsGenericType: true } named:
                return CreateGenericTypeRefFactory(named);

            default:
                return UnsupportedSymbol(type, InterpolatedExpressionTree.Unsupported);
        }
    }

    private InterpolatedExpressionTree CreateGenericTypeRefFactory(INamedTypeSymbol type) {
        var typeParameters = TypeSymbolHelpers.GetInheritedTypeParameters(type);

        var reparametrized = TypeSymbolHelpers.CreateReparametrizedTypeName(type, typeParameters, nullAnnotate: true);
        var methodName = $"CreateTypeRef{_typeRefs.Count - 1}";
        var typeArguments = Enumerable.Range(0, typeParameters.Count).MkString("<", i => $"T{i}", ", ", ">");

        _methodDefinitions.Add(InterpolatedExpressionTree.MethodDefinition(
            $"static global::Arborist.Internal.TypeRef<{reparametrized}> {methodName}{typeArguments}",
            [..(
                from i in Enumerable.Range(0, typeParameters.Count)
                let typeParameter = typeParameters[i]
                select InterpolatedExpressionTree.Verbatim($"global::Arborist.Interpolation.Internal.TypeRef<T{i}> t{i}")
            )],
            [..(
                from constraint in TypeSymbolHelpers.GetReparametrizedTypeConstraints(typeParameters)
                select InterpolatedExpressionTree.Verbatim(constraint)
            )],
            InterpolatedExpressionTree.ArrowBody(InterpolatedExpressionTree.Verbatim("default!"))
        ));

        return InterpolatedExpressionTree.StaticCall(
            methodName,
            [..TypeSymbolHelpers.GetInheritedTypeArguments(type).Select(CreateTypeRef)]
        );
    }

    public InterpolatedExpressionTree CreateTypeArray(IEnumerable<ITypeSymbol> types) =>
        types switch {
            IReadOnlyCollection<ITypeSymbol> { Count: 0 } => InterpolatedExpressionTree.Verbatim("global::System.Type.EmptyTypes"),
            _ => InterpolatedExpressionTree.Concat(
                InterpolatedExpressionTree.Verbatim("new global::System.Type[] "),
                InterpolatedExpressionTree.Initializer(types.Select(CreateType).ToList())
            )
        };

    public InterpolatedExpressionTree CreateMethodInfo(IMethodSymbol method, InvocationExpressionSyntax? node) {
        if(_methodInfos.TryGetValue(method, out var cached))
            return InterpolatedExpressionTree.Verbatim(cached.Identifier);

        var definition = _definitionFactory.Create($"__m{_methodInfos.Count}");
        _methodInfos[method] = definition;
        try {
            definition.SetInitializer(CreateMethodInfoUncached(method, node));
            return InterpolatedExpressionTree.Verbatim(definition.Identifier);
        } catch {
            _methodInfos.Remove(method);
            throw;
        }
    }

    private InterpolatedExpressionTree CreateMethodInfoUncached(IMethodSymbol method, InvocationExpressionSyntax? node) {
        if(method.IsGenericMethod)
            return InterpolatedExpressionTree.StaticCall(
                "global::Arborist.ExpressionOnNone.GetMethodInfo",
                [InterpolatedExpressionTree.Lambda([], CreateGenericMethodCall(method, node))]
            );

        var declaringType = CreateType(method.ContainingType);
        var parameterTypes = CreateTypeArray(method.Parameters.Select(p => p.Type));

        return InterpolatedExpressionTree.Concat(
            InterpolatedExpressionTree.InstanceCall(declaringType, "GetMethod", [
                InterpolatedExpressionTree.Verbatim($"\"{method.Name}\""),
                parameterTypes
            ]),
            InterpolatedExpressionTree.Verbatim("!")
        );
    }

    private InterpolatedExpressionTree CreateGenericMethodCall(IMethodSymbol method, InvocationExpressionSyntax? node) {
        // Roslyn does a stupid thing where it represents extension methods as instance
        // methods, which makes dealing with them a pain in the ass.
        if(method.ReducedFrom is not null)
            return CreateGenericMethodCall(method.ReducedFrom.Construct(method.TypeArguments, method.TypeArgumentNullableAnnotations), node);

        var typeArgs = CreateGenericMethodCallTypeArgs(method, node);
        var valueArgs = method.Parameters.Select(p => CreateDefaultValue(p.Type)).ToList();

        if(method.IsStatic) {
            if(!TypeSymbolHelpers.TryCreateTypeName(method.ContainingType, out var containingTypeName))
                return _diagnostics.UnsupportedType(method.ContainingType, InterpolatedExpressionTree.Unsupported);

            return InterpolatedExpressionTree.StaticCall(
                $"{containingTypeName}.{method.Name}{typeArgs}",
                valueArgs
            );
        } else {
            return InterpolatedExpressionTree.InstanceCall(
                CreateDefaultValue(method.ContainingType),
                $"{method.Name}{typeArgs}",
                valueArgs
            );
        }
    }

    private string CreateGenericMethodCallTypeArgs(IMethodSymbol methodSymbol, InvocationExpressionSyntax? node) {
        switch(node) {
            // Only specify type arguments if they were explicitly specified in the original call
            // (in which case we know they are nameable).
            case { Expression: MemberAccessExpressionSyntax { Name: GenericNameSyntax } }:
                var typeArgNames = new List<string>(methodSymbol.TypeArguments.Length);
                foreach(var typeArg in methodSymbol.TypeArguments) {
                    if(TypeSymbolHelpers.TryCreateTypeName(typeArg, out var typeArgName)) {
                        typeArgNames.Add(typeArgName);
                    } else {
                        return _diagnostics.UnsupportedType(typeArg, "???");
                    }
                }

                return typeArgNames.MkString("<", ", ", ">");

            default:
                return "";
        }
    }

    public InterpolatedExpressionTree CreateParameter(ITypeSymbol type, string name) {
        var cacheKey = (name, type);
        if(_parameters.TryGetValue(cacheKey, out var cached))
            return InterpolatedExpressionTree.Verbatim(cached.Identifier);

        var definition = _definitionFactory.Create($"__p{_parameters.Count}");
        _parameters[cacheKey] = definition;
        try {
            var parameterExpression = CreateExpression(nameof(Expression.Parameter), [
                CreateType(type),
                InterpolatedExpressionTree.Verbatim($"\"{name}\"")
            ]);

            definition.SetInitializer(parameterExpression);
            return InterpolatedExpressionTree.Verbatim(definition.Identifier);
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
