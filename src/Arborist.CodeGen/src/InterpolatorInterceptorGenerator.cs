using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StringBuilder = System.Text.StringBuilder;

namespace Arborist.CodeGen;

[Generator]
public class InterpolatorInterceptorGenerator : IIncrementalGenerator {
    public const int MAX_DELEGATE_PARAMETER_COUNT = 5;

    public Action<InterpolatorAnalysisResults>? AnalysisResultHandler { get; set; }

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var targetInvocations = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is InvocationExpressionSyntax,
            SyntaxTransform
        )
        .Where(static tup => tup.HasValue)
        .Select(static (tup, _) => tup!.Value)
        .Collect();

        var compilationAndInvocations = context.CompilationProvider.Combine(targetInvocations);

        context.RegisterSourceOutput(compilationAndInvocations, GenerateSources);
    }

    private static (InvocationExpressionSyntax, IMethodSymbol)? SyntaxTransform(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken
    ) {
        if(context.Node is not InvocationExpressionSyntax invocation)
            throw new ArgumentException($"Expected {nameof(InvocationExpressionSyntax)}.");
        if(context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
            return null;
        if(!methodSymbol.GetAttributes().Any(IsExpressionInterpolatorAttribute))
            return null;

        return (invocation, methodSymbol);
    }

    private static bool IsExpressionInterpolatorAttribute(AttributeData a) =>
        a.AttributeClass is {
            Name: "CompileTimeExpressionInterpolatorAttribute",
            ContainingNamespace: {
                Name: "Internal",
                ContainingNamespace: {
                    Name: "Interpolation",
                    ContainingNamespace: {
                        Name: "Arborist",
                        ContainingNamespace: { IsGlobalNamespace: true }
                    }
                }
            }
        };

    private void GenerateSources(
        SourceProductionContext sourceProductionContext,
        (Compilation, ImmutableArray<(InvocationExpressionSyntax, IMethodSymbol)>) inputs
    ) {
        try {
            var (compilation, invocations) = inputs;

            var typeSymbols = InterpolatorTypeSymbols.Create(compilation);
            var analyses = ProcessInvocations(sourceProductionContext, compilation, invocations, typeSymbols)
            .Where(static a => a.IsSupported)
            .ToList();

            if(analyses.Count != 0)
                GenerateInterceptors(compilation, sourceProductionContext, analyses);
        } catch(Exception ex) {
            sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "ARB000",
                    title: "",
                    messageFormat: $"An exception occurred: {ex.GetType()} {ex} {ex.StackTrace}",
                    defaultSeverity: DiagnosticSeverity.Error,
                    category: "Error",
                    isEnabledByDefault: true
                ),
                location: default
            ));
        }
    }

    private void GenerateInterceptors(
        Compilation compilation,
        SourceProductionContext sourceProductionContext,
        IReadOnlyList<InterpolatorAnalysisResults> analyses
    ) {
        var sb = new StringBuilder();
        GenerateInterceptsLocationAttribute(sb);

        sb.AppendLine("");
        sb.AppendLine("namespace Arborist.Interpolation.Interceptors {");

        var assemblyName = compilation.AssemblyName;
        var className = $"{assemblyName}.InterpolatorInterceptors".Replace("_", "__").Replace(".", "_");
        sb.AppendLine($"    file static class {className} {{");

        foreach(var analysis in analyses) {
            sb.AppendLine("");
            GenerateInterceptor(analysis, sb);
        }

        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");

        sourceProductionContext.AddSource(
            hintName: $"Arborist.Interpolation.Interceptors.{className}.cs",
            source: sb.ToString()
        );
    }

    private void GenerateInterceptsLocationAttribute(StringBuilder sb) {
        sb.AppendLine("namespace System.Runtime.CompilerServices {");
        sb.AppendLine("    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]");
        sb.AppendLine("    file sealed class InterceptsLocationAttribute : global::System.Attribute {");
        sb.AppendLine("        public InterceptsLocationAttribute(string filePath, int line, int column) { }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private void GenerateInterceptor(
        InterpolatorAnalysisResults analysis,
        StringBuilder sb
    ) {
        // Create a typeref for the data parameter type so it gets emitted with the other definitions
        if(analysis.DataParameter is not null)
            analysis.Builder.CreateTypeRef(analysis.DataParameter.Type);

        var resultDelegateType = ((INamedTypeSymbol)analysis.MethodSymbol.OriginalDefinition.ReturnType).TypeArguments[0];
        var typeParameters = TypeSymbolHelpers.GetInheritedTypeParameters(analysis.MethodSymbol.OriginalDefinition.ContainingType)
        .AddRange(analysis.MethodSymbol.OriginalDefinition.TypeParameters);

        var returnStatement = InterpolatedTree.Concat(
            InterpolatedTree.Verbatim("return "),
            analysis.Builder.CreateExpression(
                $"{nameof(Expression.Lambda)}<{TypeSymbolHelpers.CreateReparametrizedTypeName(resultDelegateType, "T", typeParameters, nullAnnotate: true)}>",
                [analysis.BodyTree, ..analysis.ParameterTrees]
            ),
            InterpolatedTree.Verbatim(";")
        );

        var filePath = analysis.Invocation.SyntaxTree.FilePath;
        var identifier = ((MemberAccessExpressionSyntax)analysis.Invocation.Expression).Name.Identifier;
        var linePosition = identifier.GetLocation().GetMappedLineSpan().StartLinePosition;
        var line = linePosition.Line + 1;
        var character = linePosition.Character + 1;
        sb.AppendLine($@"        [global::System.Runtime.CompilerServices.InterceptsLocation(@""{filePath.Replace("\"", "\"\"")}"", line: {line}, column: {character})]");

        var declaration = GenerateInterpolatorDeclaration(analysis);
        sb.AppendLine(declaration.ToString(2));
        sb.AppendLine($"        {{");

        foreach(var definition in analysis.Builder.ValueDefinitions) {
            var definitionTree = InterpolatedTree.Concat(
                InterpolatedTree.Verbatim($"var {definition.Identifier} = "),
                definition.Initializer,
                InterpolatedTree.Verbatim(";")
            );

            sb.AppendLine(definitionTree.ToString(3));
        }

        if(analysis.DataParameter is not null)
            sb.AppendLine($"            var {analysis.Builder.DataIdentifier} = {analysis.Builder.CreateTypeRef(analysis.DataParameter.Type)}.Cast({analysis.DataParameter.Name});");

        sb.AppendLine("");
        sb.AppendLine(returnStatement.ToString(3));

        foreach(var methodDefinition in analysis.Builder.MethodDefinitions) {
            sb.AppendLine("");
            sb.AppendLine(methodDefinition.ToString(3));
        }

        sb.AppendLine($"        }}");
    }

    private InterpolatedTree GenerateInterpolatorDeclaration(
        InterpolatorAnalysisResults analysis
    ) {
        var typeParameters = TypeSymbolHelpers.GetInheritedTypeParameters(analysis.MethodSymbol.ContainingType.OriginalDefinition)
        .AddRange(analysis.MethodSymbol.OriginalDefinition.TypeParameters);

        var typeParameterDeclarations = typeParameters.Count switch {
            0 => "",
            _ => typeParameters.Select((t, i) => $"T{i}").MkString("<", ", ", ">")
        };

        var returnType = analysis.MethodSymbol.ReturnsVoid switch {
            true => "void",
            false => TypeSymbolHelpers.CreateReparametrizedTypeName(analysis.MethodSymbol.OriginalDefinition.ReturnType, "T", typeParameters)
        };

        return InterpolatedTree.MethodDefinition(
            $"internal static {returnType} Interpolate{analysis.InvocationId}{typeParameterDeclarations}",
            [..(
                from parameter in (analysis.MethodSymbol.ReducedFrom ?? analysis.MethodSymbol).OriginalDefinition.Parameters
                let parameterTypeName = TypeSymbolHelpers.CreateReparametrizedTypeName(parameter.Type, "T", typeParameters)
                select InterpolatedTree.Verbatim($"{parameterTypeName} {parameter.Name}")
            )],
            [..(
                from constraint in TypeSymbolHelpers.GetReparametrizedTypeConstraints("T", typeParameters)
                select InterpolatedTree.Verbatim(constraint)
            )],
            // Use an empty body, as we will emit our own body elsewhere
            InterpolatedTree.Verbatim("")
        );
    }

    private IEnumerable<InterpolatorAnalysisResults> ProcessInvocations(
        SourceProductionContext sourceProductionContext,
        Compilation compilation,
        ImmutableArray<(InvocationExpressionSyntax, IMethodSymbol)> invocations,
        InterpolatorTypeSymbols typeSymbols
    ) {
        foreach(var (invocation, methodSymbol) in invocations) {
            // Assert that the input invocation actually calls a method with [ExpressionInterpolator]
            var methodAttrs = methodSymbol.GetAttributes();
            if(!methodAttrs.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, typeSymbols.CompileTimeExpressionInterpolatorAttribute)))
                continue;

            switch(invocation.ArgumentList.Arguments.Count) {
                case 1 when TryGetLambdaParameter(sourceProductionContext, methodSymbol, methodSymbol.Parameters[0], out var dataType, typeSymbols)
                    && dataType is null:
                    yield return ProcessInterpolatorInvocation(
                        sourceProductionContext,
                        compilation,
                        invocation,
                        methodSymbol,
                        default,
                        methodSymbol.Parameters[0],
                        typeSymbols
                    );
                    break;

                case 2 when TryGetLambdaParameter(sourceProductionContext, methodSymbol, methodSymbol.Parameters[1], out var dataType, typeSymbols)
                    && TypeSymbolHelpers.IsSubtype(dataType, methodSymbol.Parameters[0].Type):
                    yield return ProcessInterpolatorInvocation(
                        sourceProductionContext,
                        compilation,
                        invocation,
                        methodSymbol,
                        methodSymbol.Parameters[0],
                        methodSymbol.Parameters[1],
                        typeSymbols
                    );
                    break;

                default:
                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        descriptor: new DiagnosticDescriptor(
                            id: DiagnosticFactory.ARB999_UnsupportedInterpolatorInvocation,
                            title: "Unhandled expression interpolator method signature",
                            messageFormat: "",
                            category: "Design",
                            defaultSeverity: DiagnosticSeverity.Warning,
                            isEnabledByDefault: true
                        ),
                        location: invocation.GetLocation()
                    ));
                    break;
            }
        }
    }

    private InterpolatorAnalysisResults ProcessInterpolatorInvocation(
        SourceProductionContext sourceProductionContext,
        Compilation compilation,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol,
        IParameterSymbol? dataParameter,
        IParameterSymbol expressionParameter,
        InterpolatorTypeSymbols typeSymbols
    ) {
        var diagnostics = new DiagnosticFactory(sourceProductionContext, invocation);
        var builder = new InterpolatedTreeBuilder(diagnostics);

        // Get the syntax node for the lambda expression to be interpolated
        var interpolatedExpression = (LambdaExpressionSyntax)invocation.ArgumentList.Arguments[expressionParameter.Ordinal].Expression;
        var interpolatedExpressionParameters = interpolatedExpression switch {
            SimpleLambdaExpressionSyntax =>
                (IReadOnlyList<ParameterSyntax>)Array.Empty<ParameterSyntax>(),
            ParenthesizedLambdaExpressionSyntax parenthesized =>
                parenthesized.ParameterList.Parameters.Skip(1).ToList(),
            _ => throw new NotImplementedException()
        };

        var context = new InterpolatorInvocationContext(
            sourceProductionContext: sourceProductionContext,
            compilation: compilation,
            typeSymbols: typeSymbols,
            diagnostics: diagnostics,
            builder: builder,
            invocationSyntax: invocation,
            methodSymbol: methodSymbol,
            dataParameter: dataParameter,
            expressionParameter: expressionParameter,
            interpolatedExpression: interpolatedExpression,
            interpolatedExpressionParameters: interpolatedExpressionParameters
        );

        var expressionType = (INamedTypeSymbol)methodSymbol.ReturnType;
        var delegateType = (INamedTypeSymbol)expressionType.TypeArguments[0];

        // Create trees for each parameter to the result expression
        var parameterTrees = new List<InterpolatedTree>();
        foreach(var (parameterSyntax, parameterIndex) in interpolatedExpressionParameters.ZipWithIndex())
            parameterTrees.Add(builder.CreateParameter(
                delegateType.TypeArguments[parameterIndex],
                parameterSyntax.Identifier.Text
            ));

        var visitor = new InterpolatedSyntaxVisitor(context, builder);
        var bodyTree = visitor.Visit(interpolatedExpression.Body);

        // Report an interpolated expression containing no splices, which is completely useless
        if(context.SpliceCount == 0)
            diagnostics.NoSplices(interpolatedExpression);

        var analysisResults = new InterpolatorAnalysisResults(
            invocationContext: context,
            parameterTrees: parameterTrees,
            bodyTree: bodyTree
        );

        // Invoke any configured handler with the analysis results
        AnalysisResultHandler?.Invoke(analysisResults);

        return analysisResults;
    }

    /// <summary>
    /// Gets the <see cref="ParameterSyntax"/> nodes from the provided interpolated
    /// <paramref name="expressionSyntax"/> to be included in the output expression,
    /// discarding the interpolation context parameter.
    /// </summary>
    private IReadOnlyList<ParameterSyntax> GetInterpolatedExpressionParameters(
        LambdaExpressionSyntax expressionSyntax
    ) {
        switch(expressionSyntax) {
            case SimpleLambdaExpressionSyntax lambda:
                return Array.Empty<ParameterSyntax>();
            case ParenthesizedLambdaExpressionSyntax lambda:
                return lambda.ParameterList.Parameters.Skip(1).ToList();
            default:
                throw new NotImplementedException();
        }
    }

    private static bool TryGetLambdaParameter(
        SourceProductionContext context,
        IMethodSymbol methodSymbol,
        IParameterSymbol parameter,
        out ITypeSymbol? dataTypeSymbol,
        InterpolatorTypeSymbols typeSymbols
    ) {
        dataTypeSymbol = default;

        if(parameter.Type is not INamedTypeSymbol parameterType)
            return false;
        if(!parameterType.IsGenericType)
            return false;
        if(!SymbolEqualityComparer.Default.Equals(parameterType.ConstructUnboundGenericType(), typeSymbols.Expression1.ConstructUnboundGenericType()))
            return false;

        if(parameterType.TypeArguments[0] is not INamedTypeSymbol { IsGenericType: true } interpolatedDelegateType)
            return false;
        if(!TypeSymbolHelpers.IsSubtype(interpolatedDelegateType.TypeArguments[0], typeSymbols.IInterpolationContext))
            return false;
        if(!TryGetResultDelegateTypeFromInterpolated(interpolatedDelegateType, out var resultType, typeSymbols, context))
            return false;

        if(!TypeSymbolHelpers.IsSubtype(methodSymbol.ReturnType, typeSymbols.Expression1.ConstructedFrom.Construct(resultType)))
            return false;

        if(TypeSymbolHelpers.TryGetInterfaceImplementation(typeSymbols.IInterpolationContext1, interpolatedDelegateType.TypeArguments[0], out var ic1Impl))
            dataTypeSymbol = ic1Impl.TypeArguments[0];

        return true;
    }

    private static bool TryGetResultDelegateTypeFromInterpolated(
        INamedTypeSymbol interpolated,
        [NotNullWhen(true)] out INamedTypeSymbol? result,
        InterpolatorTypeSymbols typeSymbols,
        SourceProductionContext context
    ) {
        result = default;
        if(!interpolated.IsGenericType)
            return false;

        var unbound = interpolated.ConstructUnboundGenericType();
        var argCount = interpolated.TypeArguments.Length;

        if(2 <= argCount && SymbolEqualityComparer.Default.Equals(unbound, typeSymbols.Funcs[argCount - 1])) {
            var typeArgs = interpolated.TypeArguments.Skip(1).ToArray();
            result = typeSymbols.Funcs[argCount - 2].ConstructedFrom.Construct(typeArgs);
            return true;
        }

        if(1 <= argCount && SymbolEqualityComparer.Default.Equals(unbound, typeSymbols.Actions[argCount])) {
            var typeArgs = interpolated.TypeArguments.Skip(1).ToArray();
            var actionType = typeSymbols.Actions[argCount - 1];
            result = actionType.IsGenericType switch {
                true => actionType.ConstructedFrom.Construct(typeArgs),
                false => actionType
            };
            return true;
        }

        return false;
    }
}
