using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Arborist.CodeGen;

[Generator]
public class InterpolatorInterceptorGenerator : IIncrementalGenerator {
    public const int MAX_DELEGATE_PARAMETER_COUNT = 5;

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
            Name: "ExpressionInterpolatorAttribute",
            ContainingNamespace: {
                Name: "Interpolation",
                ContainingNamespace: {
                    Name: "Arborist",
                    ContainingNamespace: { IsGlobalNamespace: true }
                }
            }
        };

    private static void GenerateSources(
        SourceProductionContext context,
        (Compilation, ImmutableArray<(InvocationExpressionSyntax, IMethodSymbol)>) inputs
    ) {
        var (compilation, invocations) = inputs;
        var typeSymbols = new InterpolatorTypeSymbols(compilation);

        foreach(var (invocation, methodSymbol) in invocations) {
            // Assert that the input invocation actually calls a method with [ExpressionInterpolator]
            var methodAttrs = methodSymbol.GetAttributes();
            if(!methodAttrs.Any(a => typeSymbols.ExpressionInterpolatorAttribute.Equals(a.AttributeClass, SymbolEqualityComparer.Default)))
                continue;

            switch(invocation.ArgumentList.Arguments.Count) {
                case 1 when TryGetLambdaParameter(methodSymbol, methodSymbol.Parameters[0], out var dataType, typeSymbols)
                    && dataType is null:
                    ProcessInterpolatorInvocation(
                        context,
                        compilation,
                        invocation,
                        methodSymbol,
                        default,
                        methodSymbol.Parameters[0],
                        typeSymbols
                    );
                    break;

                case 2 when TryGetLambdaParameter(methodSymbol, methodSymbol.Parameters[1], out var dataType, typeSymbols)
                    && TypeSymbolHelpers.IsSubtype(dataType, methodSymbol.Parameters[0].Type):
                    ProcessInterpolatorInvocation(
                        context,
                        compilation,
                        invocation,
                        methodSymbol,
                        methodSymbol.Parameters[0],
                        methodSymbol.Parameters[1],
                        typeSymbols
                    );
                    break;

                default:
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: new DiagnosticDescriptor(
                            id: "ARB000",
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

    private static void ProcessInterpolatorInvocation(
        SourceProductionContext context,
        Compilation compilation,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol,
        IParameterSymbol? dataParameter,
        IParameterSymbol lambdaParameter,
        InterpolatorTypeSymbols typeSymbols
    ) {
        var visitorContext = new InterpolatorInvocationContext(
            sourceProductionContext: context,
            compilation: compilation,
            invocationSyntax: invocation,
            methodSymbol: methodSymbol,
            typeSymbols: typeSymbols
        );

        var lambdaSyntax = (LambdaExpressionSyntax)invocation.ArgumentList.Arguments[lambdaParameter.Ordinal].Expression;

        var output = new InterpolatorExpressionSyntaxVisitor(lambdaSyntax, visitorContext).Visit(lambdaSyntax.Body);

        //context.AddSource(invocation.GetLocation().ToString(), $"// {output}");
    }

    private static bool TryGetLambdaParameter(
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
        if(!SymbolEqualityComparer.Default.Equals(parameterType.ConstructUnboundGenericType(), typeSymbols.Expression1))
            return false;

        if(parameterType.TypeArguments[0] is not INamedTypeSymbol { IsGenericType: true } interpolatedDelegateType)
            return false;
        if(!TypeSymbolHelpers.IsSubtype(interpolatedDelegateType.TypeArguments[0], typeSymbols.IInterpolationContext))
            return false;
        if(!TryGetResultDelegateTypeFromInterpolated(interpolatedDelegateType, out var resultType, typeSymbols))
            return false;
        if(!TypeSymbolHelpers.IsSubtype(methodSymbol.ReturnType, typeSymbols.Expression1.Construct(resultType)))
            return false;

        if(TypeSymbolHelpers.TryGetInterfaceImplementation(typeSymbols.IInterpolationContext1, interpolatedDelegateType.TypeArguments[0], out var ic1Impl))
            dataTypeSymbol = ic1Impl.TypeArguments[0];

        return true;
    }

    private static bool TryGetResultDelegateTypeFromInterpolated(
        INamedTypeSymbol interpolated,
        out INamedTypeSymbol result,
        InterpolatorTypeSymbols typeSymbols
    ) {
        var unbound = interpolated.IsGenericType switch {
            true => interpolated.ConstructUnboundGenericType(),
            false => interpolated
        };

        foreach(var delegateTypes in new[] { typeSymbols.Funcs, typeSymbols.Actions }) {
            for(var i = interpolated.TypeArguments.Length; i < delegateTypes.Length; i++) {
                if(!SymbolEqualityComparer.Default.Equals(delegateTypes[i], unbound))
                    continue;

                result = delegateTypes[i - 1].Construct(interpolated.TypeArguments.Skip(1).ToArray());
                return true;
            }
        }

        result = default!;
        return false;
    }
}
