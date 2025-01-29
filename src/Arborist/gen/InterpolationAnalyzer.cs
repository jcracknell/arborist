using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Arborist.Interpolation.InterceptorGenerator;

public static class InterpolationAnalyzer {
    public static InterpolationAnalysisResult? Analyze(
        SemanticModel semanticModel,
        InterpolationDiagnosticsCollector diagnostics,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken
    ) {
        if(semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
            return default;

        var typeSymbols = InterpolationTypeSymbols.Create(semanticModel.Compilation);

        // Assert that the input invocation actually calls a method with [CompileTimeExpressionInterpolator]
        var methodAttrs = methodSymbol.GetAttributes();
        if(!methodAttrs.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, typeSymbols.CompileTimeExpressionInterpolatorAttribute)))
            return default;

        switch(invocation.ArgumentList.Arguments.Count) {
            case 2 when TryGetLambdaParameter(methodSymbol, methodSymbol.Parameters[1], out var dataType, typeSymbols)
                && TypeSymbolHelpers.IsSubtype(dataType, methodSymbol.Parameters[0].Type):
                return AnalyzeInterpolation(
                    semanticModel,
                    diagnostics,
                    invocation,
                    methodSymbol,
                    methodSymbol.Parameters[0],
                    methodSymbol.Parameters[1],
                    typeSymbols,
                    cancellationToken
                );

            default:
                diagnostics.UnsupportedInvocationSyntax(invocation);
                return default;
        }
    }

    private static InterpolationAnalysisResult? AnalyzeInterpolation(
        SemanticModel semanticModel,
        InterpolationDiagnosticsCollector diagnostics,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol,
        IParameterSymbol dataParameter,
        IParameterSymbol expressionParameter,
        InterpolationTypeSymbols typeSymbols,
        CancellationToken cancellationToken
    ) {
        var treeBuilder = new InterpolatedTreeBuilder(diagnostics);

        // Get the syntax node for the lambda expression to be interpolated
        var interpolatedExpression = (LambdaExpressionSyntax)invocation.ArgumentList.Arguments[expressionParameter.Ordinal].Expression;

        var context = new InterpolationAnalysisContext(
            invocation: invocation,
            semanticModel: semanticModel,
            typeSymbols: typeSymbols,
            diagnostics: diagnostics,
            treeBuilder: treeBuilder,
            interpolatedExpression: interpolatedExpression,
            cancellationToken: cancellationToken
        );

        var bodyTree = new InterpolatedSyntaxVisitor(context).Visit(interpolatedExpression.Body);

        // Report an interpolated expression containing no splices, which is completely useless
        if(context.SpliceCount == 0)
            diagnostics.NoSplices(interpolatedExpression);

        var dataTypeRef = treeBuilder.CreateTypeRef(dataParameter.Type);
        var dataDeclaration = InterpolatedTree.Interpolate($"var {treeBuilder.DataIdentifier} = {dataTypeRef}.Cast({dataParameter.Name});");

        var typeParameters = TypeSymbolHelpers.GetInheritedTypeParameters(methodSymbol.ContainingType.OriginalDefinition)
        .AddRange(methodSymbol.OriginalDefinition.TypeParameters);
        
        var typeParameterMappings = typeParameters.ZipWithIndex().ToDictionary(
            tup => (ITypeParameterSymbol)tup.Value.WithNullableAnnotation(NullableAnnotation.None),
            tup => $"T{tup.Index}",
            (IEqualityComparer<ITypeParameterSymbol>)SymbolEqualityComparer.Default
        );

        var returnStatement = GenerateReturnStatement(treeBuilder, methodSymbol, typeParameterMappings, bodyTree, interpolatedExpression);

        var invocationId = GenerateInvocationId(invocation);
        var className = $"InterpolationInterceptor{invocationId}";
        var assemblyName = semanticModel.Compilation.AssemblyName;
        var fileBaseName = $"{assemblyName?.Replace("_", "__").Replace('.', '_')}_{className}";

        return new InterpolationAnalysisResult(
            invocationLocation: invocation.GetLocation(),
            fileName: $"{InterpolationInterceptorGenerator.INTERCEPTOR_NAMESPACE}.{fileBaseName}.g.cs",
            className: className,
            interceptsLocationAttribute: GenerateInterceptsLocationAttribute(invocation),
            interceptorMethodDeclaration: GenerateInterceptorMethodDeclaration(treeBuilder, invocationId, methodSymbol, typeParameters, typeParameterMappings),
            bodyTree: bodyTree,
            dataDeclaration: dataDeclaration,
            returnStatement: returnStatement,
            valueDefinitions: treeBuilder.ValueDefinitions.ToList(),
            methodDefinitions: treeBuilder.MethodDefinitions.ToList()
        );
    }

    private static InterpolatedTree GenerateReturnStatement(
        InterpolatedTreeBuilder builder,
        IMethodSymbol methodSymbol,
        IReadOnlyDictionary<ITypeParameterSymbol, string> typeParameterMappings,
        InterpolatedTree bodyTree,
        LambdaExpressionSyntax interpolatedExpression
    ) {
        var interpolatedExpressionParameters = GetInterpolatedExpressionParameters(interpolatedExpression);

        var expressionType = (INamedTypeSymbol)methodSymbol.ReturnType;
        var delegateType = (INamedTypeSymbol)expressionType.TypeArguments[0];

        // Create trees for each parameter to the result expression
        var parameterTrees = new List<InterpolatedTree>(interpolatedExpressionParameters.Count);
        foreach(var (parameterSyntax, parameterIndex) in interpolatedExpressionParameters.ZipWithIndex())
            parameterTrees.Add(builder.CreateParameter(
                delegateType.TypeArguments[parameterIndex],
                parameterSyntax.Identifier.Text
            ));

        var resultDelegateType = ((INamedTypeSymbol)methodSymbol.OriginalDefinition.ReturnType).TypeArguments[0];
        var reparametrizedDelegateType = builder.CreateTypeName(resultDelegateType, interpolatedExpression, typeParameterMappings);
        if(!reparametrizedDelegateType.IsSupported)
            return InterpolatedTree.Unsupported;
        
        return InterpolatedTree.Concat(
            InterpolatedTree.Verbatim("return "),
            builder.CreateExpression(
                $"{nameof(Expression.Lambda)}<{reparametrizedDelegateType}>",
                [bodyTree, ..parameterTrees]
            ),
            InterpolatedTree.Verbatim(";")
        );
    }

    private static InterpolatedTree GenerateInterceptsLocationAttribute(InvocationExpressionSyntax invocation) {
        // This always succeeds because it was used in the source generator input filter
        if(!InterpolationInterceptorGenerator.TryGetInvocationMethodIdentifier(invocation, out var identifier))
            throw new NotImplementedException();

        const string attributeName = "global::System.Runtime.CompilerServices.InterceptsLocation";
        var filePath = invocation.SyntaxTree.FilePath.Replace("\"", "\"\"");
        var linePosition = identifier.GetLocation().GetMappedLineSpan().StartLinePosition;
        var line = linePosition.Line + 1;
        var character = linePosition.Character + 1;

        return InterpolatedTree.Verbatim($@"[{attributeName}(@""{filePath}"", line: {line}, column: {character})]");
    }

    private static InterpolatedTree GenerateInterceptorMethodDeclaration(
        InterpolatedTreeBuilder builder,
        string invocationId,
        IMethodSymbol methodSymbol,
        ImmutableList<ITypeParameterSymbol> typeParameters,
        IReadOnlyDictionary<ITypeParameterSymbol, string> typeParameterMappings
    ) {
        var returnType = builder.CreateTypeName(methodSymbol.OriginalDefinition.ReturnType, default, typeParameterMappings);

        var typeParameterDeclarations = typeParameters.Count switch {
            0 => "",
            _ => typeParameters.Select((t, i) => $"T{i}").MkString("<", ", ", ">")
        };

        return InterpolatedTree.MethodDefinition(
            InterpolatedTree.Interpolate($"internal static {returnType} Interpolate{invocationId}{typeParameterDeclarations}"),
            [..(
                from parameter in (methodSymbol.ReducedFrom ?? methodSymbol).OriginalDefinition.Parameters
                let parameterTypeName = builder.CreateTypeName(parameter.Type, default, typeParameterMappings)
                select InterpolatedTree.Interpolate($"{parameterTypeName} {parameter.Name}")
            )],
            builder.GetReparametrizedTypeConstraints(typeParameters, typeParameterMappings),
            // Use an empty body, as we will emit our own body elsewhere
            InterpolatedTree.Empty
        );
    }

    /// <summary>
    /// Gets the <see cref="ParameterSyntax"/> nodes from the provided interpolated
    /// <paramref name="expressionSyntax"/> to be included in the output expression,
    /// discarding the interpolation context parameter.
    /// </summary>
    private static IReadOnlyList<ParameterSyntax> GetInterpolatedExpressionParameters(
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
        IMethodSymbol methodSymbol,
        IParameterSymbol parameter,
        [NotNullWhen(true)] out ITypeSymbol? dataType,
        InterpolationTypeSymbols typeSymbols
    ) {
        dataType = default;

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
        if(!TryGetResultDelegateTypeFromInterpolated(interpolatedDelegateType, out var resultType, typeSymbols))
            return false;

        if(!TypeSymbolHelpers.IsSubtype(methodSymbol.ReturnType, typeSymbols.Expression1.ConstructedFrom.Construct(resultType)))
            return false;

        if(TypeSymbolHelpers.TryGetInterfaceImplementation(typeSymbols.IInterpolationContext1, interpolatedDelegateType.TypeArguments[0], out var ic1Impl)) {
            dataType = ic1Impl.TypeArguments[0];
            return true;
        }

        return false;
    }

    private static bool TryGetResultDelegateTypeFromInterpolated(
        INamedTypeSymbol interpolated,
        [NotNullWhen(true)] out INamedTypeSymbol? result,
        InterpolationTypeSymbols typeSymbols
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

    private static string GenerateInvocationId(InvocationExpressionSyntax node) {
        var lineSpan = node.GetLocation().GetLineSpan();
        var filePath = node.SyntaxTree.FilePath;
        var bufferSize = Math.Max(64, Encoding.UTF8.GetByteCount(filePath));
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try {
            using var hash = System.Security.Cryptography.SHA256.Create();

            // Source checksum
            if(node.SyntaxTree.TryGetText(out var sourceText)) {
                var checkSum = sourceText.GetChecksum();
                checkSum.CopyTo(buffer);
                hash.TransformBlock(buffer, 0, checkSum.Length, default, 0);
            }

            // Call position
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0), lineSpan.StartLinePosition.Line);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4), lineSpan.StartLinePosition.Character);
            hash.TransformBlock(buffer, 0, 8, default, 0);

            // UTF-8 encoded file path
            var pathBytes = Encoding.UTF8.GetBytes(filePath, 0, filePath.Length, buffer, 0);
            hash.TransformBlock(buffer, 0, pathBytes, default, 0);

            hash.TransformFinalBlock(buffer, 0, 0);

            return hash.Hash.Take(8).MkString(static b => b.ToString("x2"), "");
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
