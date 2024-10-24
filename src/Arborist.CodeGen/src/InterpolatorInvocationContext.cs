using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

internal class InterpolatorInvocationContext {
    public InterpolatorInvocationContext(
        SourceProductionContext sourceProductionContext,
        Compilation compilation,
        InvocationExpressionSyntax invocationSyntax,
        IMethodSymbol methodSymbol,
        InterpolatorTypeSymbols typeSymbols
    ) {
        SourceProductionContext = sourceProductionContext;
        Compilation = compilation;
        SemanticModel = compilation.GetSemanticModel(invocationSyntax.SyntaxTree);
        InvocationSyntax = invocationSyntax;
        MethodSymbol = methodSymbol;
        TypeSymbols = typeSymbols;
    }

    public SourceProductionContext SourceProductionContext { get; }
    public Compilation Compilation { get; }
    public SemanticModel SemanticModel { get; }
    public InvocationExpressionSyntax InvocationSyntax { get; }
    public IMethodSymbol MethodSymbol { get; }
    public InterpolatorTypeSymbols TypeSymbols { get; }

    public DiagnosticSeverity DiagnosticSeverity { get; set; } = DiagnosticSeverity.Warning;

    public void Diagnostic(
        string code,
        string title,
        string message,
        SyntaxNode? syntax
    ) {
        SourceProductionContext.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(
            descriptor: new DiagnosticDescriptor(
                id: code,
                title: title,
                messageFormat: message,
                category: "",
                defaultSeverity: DiagnosticSeverity,
                isEnabledByDefault: true
            ),
            location: (syntax ?? InvocationSyntax).GetLocation()
        ));
    }

    public A Diagnostic<A>(
        A result,
        string code,
        string title,
        string message,
        SyntaxNode? syntax
    ) {
        Diagnostic(code, title, message, syntax);
        return result;
    }
}
