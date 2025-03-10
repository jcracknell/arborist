using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Reflection;

namespace Arborist.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InterpolationAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = GetSupportedDiagnostics();

    private static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics() =>
        typeof(InterpolationDiagnosticDescriptors)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
        .Select(p => (DiagnosticDescriptor)p.GetValue(null))
        .ToImmutableArray();

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.InvocationExpression);
    }

    private static void SyntaxNodeAction(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if(!TryGetInvocationMethodIdentifier(invocation, out var identifier))
            return;
        if(!identifier.ValueText.StartsWith("Interpolate"))
            return;
        if(context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
            return;
        if(!methodSymbol.GetAttributes().Any(IsExpressionInterpolatorAttribute))
            return;

        var typeSymbols = InterpolationTypeSymbols.Create(context.SemanticModel.Compilation);
        if(!SymbolHelpers.HasAttribute(methodSymbol, typeSymbols.ExpressionInterpolatorAttribute))
            return;

        var diagnostics = new InterpolationDiagnosticsCollection();

        AnalyzeInterpolation(
            diagnostics: diagnostics,
            semanticModel: context.SemanticModel,
            invocation: invocation,
            methodSymbol: methodSymbol,
            typeSymbols: typeSymbols,
            cancellationToken: context.CancellationToken
        );

        foreach(var diagnostic in diagnostics)
            context.ReportDiagnostic(diagnostic);
    }

    internal static bool TryGetInvocationMethodIdentifier(InvocationExpressionSyntax invocation, out SyntaxToken identifier) {
        switch(invocation.Expression) {
            case MemberAccessExpressionSyntax mae:
                identifier = mae.Name.Identifier;
                return true;

            case SimpleNameSyntax sns:
                identifier = sns.Identifier;
                return true;

            default:
                identifier = default;
                return false;
        }
    }

    private static bool IsExpressionInterpolatorAttribute(AttributeData a) =>
        a.AttributeClass is {
            Name: "ExpressionInterpolatorAttribute",
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

    private static void AnalyzeInterpolation(
        InterpolationDiagnosticsCollection diagnostics,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol,
        InterpolationTypeSymbols typeSymbols,
        CancellationToken cancellationToken
    ) {
        switch(invocation.ArgumentList.Arguments.Count) {
            case 1 when TryGetLambdaParameter(methodSymbol, methodSymbol.Parameters[0], out var dataType, typeSymbols)
                && dataType is null
                && TryGetParameterArgumentSyntax(invocation, methodSymbol.Parameters[0], out var expressionArgument, semanticModel):
                AnalyzeInterpolation(
                    semanticModel: semanticModel,
                    diagnostics: diagnostics,
                    invocation: invocation,
                    expressionArgument: expressionArgument,
                    typeSymbols: typeSymbols,
                    cancellationToken: cancellationToken
                );
                break;

            case 2 when TryGetLambdaParameter(methodSymbol, methodSymbol.Parameters[1], out var dataType, typeSymbols)
                && SymbolHelpers.IsSubtype(methodSymbol.Parameters[0].Type, dataType)
                && TryGetParameterArgumentSyntax(invocation, methodSymbol.Parameters[1], out var expressionArgument, semanticModel):
                AnalyzeInterpolation(
                    semanticModel: semanticModel,
                    diagnostics: diagnostics,
                    invocation: invocation,
                    expressionArgument: expressionArgument,
                    typeSymbols: typeSymbols,
                    cancellationToken: cancellationToken
                );
                break;

            default:
                diagnostics.ReportUnsupportedSyntax(invocation);
                break;
        }
    }

    private static void AnalyzeInterpolation(
        InterpolationDiagnosticsCollection diagnostics,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        ArgumentSyntax expressionArgument,
        InterpolationTypeSymbols typeSymbols,
        CancellationToken cancellationToken
    ) {
        // If the expression is not provided as a literal lambda expression, we cannot perform the analysis
        if(expressionArgument.Expression is not LambdaExpressionSyntax lambdaSyntax)
            return;

        var context = new InterpolationAnalysisContext(
            invocation: invocation,
            semanticModel: semanticModel,
            typeSymbols: typeSymbols,
            diagnostics: diagnostics,
            lambdaSyntax: lambdaSyntax,
            cancellationToken: cancellationToken
        );

        var visitor = new InterpolationAnalysisSyntaxWalker(context);
        visitor.Apply(lambdaSyntax);

        // Report an interpolated expression containing no splices, which is completely useless.
        // In this case we still emit the interceptor, as this will be more performant than having the runtime
        // fallback rewrite the expression.
        if(!visitor.SplicesFound)
            diagnostics.ReportNoSplices(lambdaSyntax);
    }

    private static bool TryGetLambdaParameter(
        IMethodSymbol methodSymbol,
        IParameterSymbol parameter,
        out ITypeSymbol? dataType,
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
        if(!SymbolHelpers.IsSubtype(interpolatedDelegateType.TypeArguments[0], typeSymbols.IInterpolationContext))
            return false;
        if(!TryGetResultDelegateTypeFromInterpolated(interpolatedDelegateType, out var resultType, typeSymbols))
            return false;

        if(!SymbolHelpers.IsSubtype(methodSymbol.ReturnType, typeSymbols.Expression1.ConstructedFrom.Construct(resultType)))
            return false;

        if(SymbolHelpers.IsSubtype(interpolatedDelegateType.TypeArguments[0], typeSymbols.IInterpolationContext)) {
            if(SymbolHelpers.TryGetInterfaceImplementation(typeSymbols.IInterpolationContext1, interpolatedDelegateType.TypeArguments[0], out var ic1Impl)) {
                dataType = ic1Impl.TypeArguments[0];
                return true;
            }

            dataType = default;
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

    /// <summary>
    /// Attempts to locate the argument tree in the provided invocation corresponding to the provided
    /// <see cref="IParameterSymbol"/>.
    /// </summary>
    private static bool TryGetParameterArgumentSyntax(
        InvocationExpressionSyntax invocation,
        IParameterSymbol parameterSymbol,
        [NotNullWhen(true)] out ArgumentSyntax? argumentSyntax,
        SemanticModel semanticModel
    ) {
        // In most cases the argument will be provided positionally, so we'll check that first
        if(
            invocation.ArgumentList.Arguments[parameterSymbol.Ordinal] is {} positional
            && semanticModel.GetOperation(positional) is IArgumentOperation poi
            && SymbolEqualityComparer.Default.Equals(parameterSymbol, poi.Parameter)
        ) {
            argumentSyntax = positional;
            return true;
        }

        // Otherwise we'll do a linear search for the matching named argument
        foreach(var candidate in invocation.ArgumentList.Arguments) {
            if(
                candidate.NameColon is not null
                && semanticModel.GetOperation(candidate) is IArgumentOperation noi
                && SymbolEqualityComparer.Default.Equals(parameterSymbol, noi.Parameter)
            ) {
                argumentSyntax = candidate;
                return true;
            }
        }

        argumentSyntax = default;
        return false;
    }
}
