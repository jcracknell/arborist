using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Arborist.Generators;

[Generator]
public class QueryableInterpolationExtensionsSourceGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: SyntaxProviderPredicate,
            transform: SyntaxProviderTransform
        )
        .SelectMany((groups, _) => groups);

        context.RegisterSourceOutput(syntaxProvider, GenerateSource);
    }

    // This is the "least incremental" possible source generator - we'll use our placeholder partial
    // as the sole possible input and then analyze System.Linq.Queryable to generate our extension
    // method overrides.
    private static bool SyntaxProviderPredicate(SyntaxNode node, CancellationToken cancellationToken) =>
        node is ClassDeclarationSyntax { Identifier.ValueText: "QueryableInterpolationExtensions" };

    private static ImmutableArray<MethodGroup> SyntaxProviderTransform(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken
    ) {
        var symbols = new CommonSymbols(context.SemanticModel.Compilation);

        return symbols.Queryable.GetMembers().OfType<IMethodSymbol>()
        .Where(ShouldOverride)
        .GroupBy(m => m.Name)
        .OrderBy(g => g.Key)
        .Select(g => new MethodGroup(methods: g.ToList(), symbols))
        .ToImmutableArray();

        bool ShouldOverride(IMethodSymbol method) =>
            method.IsExtensionMethod
            && Accessibility.Public == method.DeclaredAccessibility
            // Accepts an expression argument
            && method.Parameters.Any(p => IsExpression(p, symbols))
            // No expression argument accepting an int input (index parameters which prevent inferral of the
            // overload accepting an interpolation context)
            && !method.Parameters.Any(
                p => TryGetExpressionDelegateTypeArgs(p.Type, symbols, out var typeArgs)
                && typeArgs.Slice(0, typeArgs.Length - 1).Any(t => SymbolEqualityComparer.Default.Equals(t, symbols.Int32))
            );
    }

    private sealed class MethodGroup(
        IReadOnlyList<IMethodSymbol> methods,
        CommonSymbols symbols
    )
        : IEquatable<MethodGroup>
    {
        public IReadOnlyList<IMethodSymbol> Methods { get; } = methods;
        public CommonSymbols Symbols { get; } = symbols;

        /// <summary>
        /// A <see cref="MethodGroup"/> can have data if there is no method in the group accepting a
        /// generic argument immediately preceding the first expression parameter.
        /// </summary>
        public bool CanHaveData =>
            Methods.All(m => m.Parameters.IndexOf(p => IsExpression(p, Symbols)) switch {
                <= 0 => true,
                var i => m.Parameters[i - 1].Type is not ITypeParameterSymbol
            });

        public override int GetHashCode() =>
            Methods[0].Name.GetHashCode();

        public override bool Equals(object? obj) =>
            Equals(obj as MethodGroup);

        public bool Equals(MethodGroup? that) =>
            that is not null
            && this.Methods[0].Name.Equals(that.Methods[0].Name);
    }

    private sealed class CommonSymbols(Compilation compilation) {
        public ITypeSymbol Queryable { get; } = compilation.GetTypeByMetadataName("System.Linq.Queryable")!;
        public ITypeSymbol Expression1 { get; } = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1")!.ConstructUnboundGenericType();
        public ITypeSymbol Int32 { get; } = compilation.GetTypeByMetadataName("System.Int32")!;
    }

    private static bool IsExpression(IParameterSymbol parameter, CommonSymbols symbols) =>
        IsExpression(parameter.Type, symbols);

    private static bool IsExpression(ITypeSymbol typeSymbol, CommonSymbols symbols) =>
        TryGetExpressionDelegateTypeArgs(typeSymbol, symbols, out _);

    /// <summary>
    /// Returns true if the the provided <paramref name="type"/> is <see cref="Expression{TDelegate}"/>
    /// with a generic delegate type.
    /// </summary>
    private static bool TryGetExpressionDelegateTypeArgs(
        ITypeSymbol type,
        CommonSymbols symbols,
        out ImmutableArray<ITypeSymbol> typeArgs
    ) {
        if(type is not INamedTypeSymbol { IsGenericType: true } named)
            return false;
        if(!SymbolEqualityComparer.Default.Equals(named.ConstructUnboundGenericType(), symbols.Expression1))
            return false;
        if(named.TypeArguments[0] is not INamedTypeSymbol { IsGenericType: true, Name: "Func" } delegateType)
            return false;

        typeArgs = delegateType.TypeArguments;
        return true;
    }

    private static void GenerateSource(SourceProductionContext context, MethodGroup methodGroup) {
        var stringBuilder = new StringBuilder(4096);
        AppendPartial(stringBuilder, methodGroup);

        context.AddSource(
            hintName: $"Arborist.QueryableInterpolationExtensions.{methodGroup.Methods[0].Name}.g.cs",
            source: stringBuilder.ToString()
        );
    }

    private static void AppendPartial(StringBuilder sb, MethodGroup methodGroup) {
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace Arborist;");
        sb.AppendLine();
        sb.AppendLine("public static partial class QueryableInterpolationExtensions {");

        foreach(var method in methodGroup.Methods) {
            var interpolatable = method.Parameters
            .Where(p => IsExpression(p, methodGroup.Symbols))
            .ToArray();

            // The easiest way to generate all permutations of interpolatable expressions is to just treat an integer as
            // a bitset, and increment up through all possible values
            var interpolated = new bool[method.Parameters.Length];
            for(var bitset = 1; bitset < 1 << interpolatable.Length; bitset++) {
                // Translate the bitset into an index-based lookup for convenience.
                // We know that only indexes corresponding to interpolatable parameters are ever altered.
                for(var i = 0; i < interpolatable.Length; i++)
                    interpolated[interpolatable[i].Ordinal] = 0 != (bitset & (1 << i));

                AppendMethodVariant(sb, method, withData: false, interpolated, methodGroup);

                if(methodGroup.CanHaveData)
                    AppendMethodVariant(sb, method, withData: true, interpolated, methodGroup);
            }
        }

        sb.AppendLine("}");
    }

    private static void AppendMethodVariant(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        bool withData,
        ReadOnlySpan<bool> interpolated,
        MethodGroup methodGroup
    ) {
        sb.AppendLine($@"    /// <inheritdoc cref=""{methodSymbol.GetDocumentationCommentId()}"" />");
        sb.AppendLine($@"    /// <seealso cref=""{methodSymbol.GetDocumentationCommentId()}"" />");
        if(withData) {
            sb.AppendLine(@"    /// <param name=""data"">Optional data provided to the interpolation process.</param>");
            sb.AppendLine(@"    /// <typeparam name=""TData"">The type of <paramref name=""data""/> provided to the interpolation process.</typeparam>");
        }

        sb.AppendLine("    [global::Arborist.Interpolation.Internal.ExpressionInterpolator]");
        sb.Append("    public static ");
        AppendTypeName(sb, methodSymbol.ReturnType);
        sb.Append($" {methodSymbol.Name}Interpolated<");
        AppendTypeName(sb, methodSymbol.TypeParameters[0]);
        sb.Append(withData switch { true => ", TData", false => "" });
        for(var i = 1; i < methodSymbol.TypeParameters.Length; i++) {
            sb.Append(", ");
            AppendTypeName(sb, methodSymbol.TypeParameters[i]);
        }
        sb.AppendLine(">(");

        sb.Append("        this ");
        AppendTypeName(sb, methodSymbol.Parameters[0].Type);
        sb.Append($" {methodSymbol.Parameters[0].Name}");

        for(var i = 1; i < methodSymbol.Parameters.Length; i++) {
            var parameter = methodSymbol.Parameters[i];
            sb.AppendLine(",");
            sb.Append("        ");

            // Emit the data parameter immediately preceding the first expression parameter
            if(withData && i == methodSymbol.Parameters.IndexOf(p => IsExpression(p, methodGroup.Symbols))) {
                sb.AppendLine("[global::Arborist.Interpolation.Internal.InterpolatedDataParameter] TData data,");
                sb.Append("        ");
            }

            if(!(
                interpolated[i]
                && TryGetExpressionDelegateTypeArgs(parameter.Type, methodGroup.Symbols, out var typeArgs)
            )) {
                AppendRefKind(sb, parameter);
                AppendTypeName(sb, parameter.Type);
                sb.Append($" {parameter.Name}");
            } else {
                sb.Append("[global::Arborist.Interpolation.Internal.InterpolatedExpressionParameter] ");
                AppendRefKind(sb, parameter);
                sb.Append("global::System.Linq.Expressions.Expression<Func<");
                sb.Append(withData switch {
                    true => "global::Arborist.Interpolation.IInterpolationContext<TData>",
                    false => "global::Arborist.Interpolation.IInterpolationContext"
                });
                foreach(var typeArg in typeArgs) {
                    sb.Append(", ");
                    AppendTypeName(sb, typeArg);
                }
                sb.Append($">> {parameter.Name}");
            }

            if(parameter.HasExplicitDefaultValue) {
                sb.Append(" = ");
                sb.Append(parameter.ExplicitDefaultValue switch {
                    null => "default",
                    true => "true",
                    false => "false",
                    _ => throw new NotImplementedException()
                });
            }
        }

        sb.AppendLine();
        sb.Append("    )");

        foreach(var typeParameter in methodSymbol.TypeParameters) {
            if(GetTypeParameterConstraints(typeParameter) is { Count: not 0 } constraints) {
                sb.AppendLine();
                sb.Append("        where ");
                AppendTypeName(sb, typeParameter);
                sb.Append(" : ");
                sb.Append(constraints[0]);
                for(var i = 1; i < constraints.Count; i++) {
                    sb.Append(", ");
                    sb.Append(constraints[i]);
                }
            }
        }

        sb.AppendLine(" =>");
        sb.AppendLine($"        global::System.Linq.Queryable.{methodSymbol.Name}(");
        sb.Append($"            {methodSymbol.Parameters[0].Name}");

        for(var i = 1; i < methodSymbol.Parameters.Length; i++) {
            var parameter = methodSymbol.Parameters[i];

            sb.AppendLine(",");
            sb.Append("            ");
            if(!(
                interpolated[i]
                && TryGetExpressionDelegateTypeArgs(parameter.Type, methodGroup.Symbols, out var inputTypes)
            )) {
                sb.Append(methodSymbol.Parameters[i].Name);
            } else {
                sb.Append("global::Arborist.ExpressionOn");
                AppendTypeParameterList(sb, inputTypes.Slice(0, inputTypes.Length - 1));
                sb.Append(".Interpolate(");
                sb.Append(withData switch { true => "data, ", false => "" });
                sb.Append(methodSymbol.Parameters[i].Name);
                sb.Append(")");
            }
        }

        sb.AppendLine();
        sb.AppendLine("        );");
        sb.AppendLine();
    }

    private static void AppendRefKind(StringBuilder sb, IParameterSymbol parameter) {
        sb.Append(parameter.RefKind switch {
            RefKind.None => "",
            RefKind.In => "in ",
            RefKind.Out => "out ",
            RefKind.RefReadOnlyParameter => "ref readonly ",
            _ => throw new NotImplementedException()
        });
    }

    private static void AppendTypeName(StringBuilder sb, ITypeSymbol typeSymbol) {
        switch(typeSymbol) {
            case ITypeParameterSymbol:
                sb.Append(typeSymbol.Name);
                sb.Append(Nullability(typeSymbol));
                return;

            case INamedTypeSymbol named:
                AppendNamespaceName(sb, named.ContainingNamespace);
                sb.Append(named.Name);
                AppendTypeParameterList(sb, named.TypeArguments);
                sb.Append(Nullability(typeSymbol));
                return;

            default:
                sb.Append("???");
                return;
        }
    }

    private static void AppendTypeParameterList(StringBuilder sb, IEnumerable<ITypeSymbol> types) {
        using var enumerator = types.GetEnumerator();
        if(!enumerator.MoveNext())
            return;

        sb.Append('<');
        AppendTypeName(sb, enumerator.Current);

        while(enumerator.MoveNext()) {
            sb.Append(", ");
            AppendTypeName(sb, enumerator.Current);
        }

        sb.Append('>');
    }

    private static void AppendNamespaceName(StringBuilder sb, INamespaceSymbol ns) {
        if(ns.IsGlobalNamespace) {
            sb.Append("global::");
        } else {
            AppendNamespaceName(sb, ns.ContainingNamespace);
            sb.Append(ns.Name);
            sb.Append('.');
        }
    }

    private static IReadOnlyList<string> GetTypeParameterConstraints(ITypeParameterSymbol typeParameter) {
        return GetConstraints(typeParameter).ToList();

        static IEnumerable<string> GetConstraints(ITypeParameterSymbol typeParameter) {
            if(typeParameter.HasValueTypeConstraint)
                yield return "struct";
            if(typeParameter.HasReferenceTypeConstraint)
                yield return $"class{Nullability(typeParameter.ReferenceTypeConstraintNullableAnnotation)}";
            if(typeParameter.HasNotNullConstraint)
                yield return "notnull";
            if(typeParameter.HasUnmanagedTypeConstraint)
                yield return "unmanaged";

            if(!typeParameter.ConstraintTypes.IsEmpty) {
                var sb = new StringBuilder();
                for(var i = 0; i < typeParameter.ConstraintTypes.Length; i++) {
                    sb.Clear();
                    AppendTypeName(sb, typeParameter.ConstraintTypes[i].WithNullableAnnotation(typeParameter.ConstraintNullableAnnotations[i]));
                    yield return sb.ToString();
                }
            }

            if(typeParameter.HasConstructorConstraint)
                yield return "new()";
        }
    }

    private static string Nullability(ITypeSymbol type) =>
        type.IsValueType ? "" : Nullability(type.NullableAnnotation);

    private static string Nullability(NullableAnnotation annotation) =>
        NullableAnnotation.Annotated == annotation ? "?" : "";
}
