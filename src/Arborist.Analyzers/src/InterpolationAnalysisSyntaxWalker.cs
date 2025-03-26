using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Analyzers;

public sealed class InterpolationAnalysisSyntaxWalker : CSharpSyntaxWalker {
    private readonly InterpolationAnalysisContext _context;

    /// <summary>
    /// The IInterpolationContext identifier, which cannot be referenced in the interpolated
    /// expression tree. May be empty in the event that the identifier is rebound/shadowed.
    /// </summary>
    private ImmutableHashSet<string> _contextIdentifier;

    /// <summary>
    /// Identifiers introduced by the interpolated expression (typically as lambda parameters)
    /// which cannot be referenced in an evaluated expression.
    /// </summary>
    private ImmutableHashSet<string> _interpolatedIdentifiers;

    private bool _inEvaluatedExpression;

    public InterpolationAnalysisSyntaxWalker(InterpolationAnalysisContext context) {
        _context = context;
        _contextIdentifier = ImmutableHashSet<string>.Empty;
        _interpolatedIdentifiers = ImmutableHashSet<string>.Empty;
    }

    public bool SplicesFound { get; private set; }

    private void AddInterpolatedIdentifier(string identifier) {
        // Handle rebinding/shadowing of the context identifier
        _contextIdentifier = _contextIdentifier.Remove(identifier);

        // If we are within an evaluated expression, any defined identifiers are accessible
        // to the evaluation process and shadow any interpolated identifiers
        _interpolatedIdentifiers = _inEvaluatedExpression switch {
            true => _interpolatedIdentifiers.Remove(identifier),
            false => _interpolatedIdentifiers.Add(identifier)
        };
    }

    private IdentifiersSnapshot CreateIdentifiersSnapshot() =>
        new IdentifiersSnapshot(
            visitor: this,
            contextSnapshot: _contextIdentifier,
            interpolatedSnapshot: _interpolatedIdentifiers
        );

    private readonly struct IdentifiersSnapshot(
        InterpolationAnalysisSyntaxWalker visitor,
        ImmutableHashSet<string> contextSnapshot,
        ImmutableHashSet<string> interpolatedSnapshot
    ) : IDisposable {
        public void Dispose() {
            visitor._contextIdentifier = contextSnapshot;
            visitor._interpolatedIdentifiers = interpolatedSnapshot;
        }
    }

    public void Apply(LambdaExpressionSyntax lambda) {
        var parameters = GetLambdaParameters(lambda);

        // Register the interpolation context parameter as a forbidden identifier
        _contextIdentifier = _contextIdentifier.Add(parameters[0].Identifier.ValueText);
        // Register all of the parameters as interpolated identifiers which cannot be referenced in an
        // evaluated expression. We don't use the helper method here to preserve the context identifier.
        _interpolatedIdentifiers = _interpolatedIdentifiers.Union(parameters.Select(p => p.Identifier.ValueText));

        Visit(lambda.Body);
    }

    private IReadOnlyList<ParameterSyntax> GetLambdaParameters(LambdaExpressionSyntax node) =>
        node switch {
            SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter },
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters,
            _ => throw new NotImplementedException()
        };

    public override void Visit(SyntaxNode? node) {
        // Check for cancellation every time we visit (iterate) over a node
        _context.CancellationToken.ThrowIfCancellationRequested();
        base.Visit(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(!_inEvaluatedExpression) {
            if(TryGetSplicingMethod(node, out var spliceMethod)) {
                VisitSplicingInvocation(node, spliceMethod);
                return;
            }

            // Report a call to an expression interpolator occurring within an interpolated expression.
            // N.B. we still need to apply any interpolations associated with our interpolation context
            // to its argument expressions.
            if(SyntaxHelpers.IsExpressionInterpolatorInvocation(node, _context.SemanticModel, out _))
                _context.Diagnostics.ReportNestedInterpolation(node);
        }

        base.VisitInvocationExpression(node);
    }

    private void VisitSplicingInvocation(InvocationExpressionSyntax node, IMethodSymbol method) {
        SplicesFound = true;

        foreach(var (argument, argumentIndex) in node.ArgumentList.Arguments.ZipWithIndex()) {
            var parameter = method.Parameters[argumentIndex];

            if(SymbolHelpers.HasAttribute(parameter, _context.TypeSymbols.EvaluatedSpliceParameterAttribute)) {
                _inEvaluatedExpression = true;
                Visit(argument);
                _inEvaluatedExpression = false;
            } else if(SymbolHelpers.HasAttribute(parameter, _context.TypeSymbols.InterpolatedSpliceParameterAttribute)) {
                Visit(argument);
            } else {
                // Parameter is not properly marked as evaluated/interpolated
                _context.Diagnostics.ReportUnsupportedSyntax(argument);
            }
        }
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        if(!_inEvaluatedExpression || !IsContextDataAccess(node)) {
            base.VisitMemberAccessExpression(node);
        }
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node) {
        // The only identifier which cannot be referenced within the interpolated expression tree
        // is the one referencing the interpolation context, which does not exist in the result
        // expression.
        if(_contextIdentifier.Contains(node.Identifier.ValueText)) {
            _context.Diagnostics.ReportInterpolationContextReference(node);
        } else if(_inEvaluatedExpression && _interpolatedIdentifiers.Contains(node.Identifier.ValueText)) {
            _context.Diagnostics.ReportInterpolatedParameterReference(node);
        }
    }

    public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
        using var snapshot = CreateIdentifiersSnapshot();

        // Register parameters declared in the lambda. This also handles shadowing of the top-level
        // interpolation context parameter (for nested interpolation calls)
        AddInterpolatedIdentifier(node.Parameter.Identifier.ValueText);

        base.VisitSimpleLambdaExpression(node);
    }

    public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        using var snapshot = CreateIdentifiersSnapshot();

        // Register parameters declared in the lambda. This also handles shadowing of the top-level
        // interpolation context parameter (for nested interpolation calls)
        foreach(var parameter in node.ParameterList.Parameters)
            AddInterpolatedIdentifier(parameter.Identifier.ValueText);

        base.VisitParenthesizedLambdaExpression(node);
    }

    public override void VisitQueryExpression(QueryExpressionSyntax node) {
        using(CreateIdentifiersSnapshot()) {
            Visit(node.FromClause);

            foreach(var clause in node.Body.Clauses)
                Visit(clause);

            Visit(node.Body.SelectOrGroup);
        }

        Visit(node.Body.Continuation);
    }

    public override void VisitQueryContinuation(QueryContinuationSyntax node) {
        using(CreateIdentifiersSnapshot()) {
            AddInterpolatedIdentifier(node.Identifier.ValueText);

            foreach(var clause in node.Body.Clauses)
                Visit(clause);

            Visit(node.Body.SelectOrGroup);
        }

        Visit(node.Body.Continuation);
    }

    public override void VisitFromClause(FromClauseSyntax node) {
        AddInterpolatedIdentifier(node.Identifier.ValueText);
        base.VisitFromClause(node);
    }

    public override void VisitJoinClause(JoinClauseSyntax node) {
        AddInterpolatedIdentifier(node.Identifier.ValueText);
        base.VisitJoinClause(node);
    }

    public override void VisitLetClause(LetClauseSyntax node) {
        AddInterpolatedIdentifier(node.Identifier.ValueText);
        base.VisitLetClause(node);
    }

    private bool TryGetSplicingMethod(
        InvocationExpressionSyntax node,
        [NotNullWhen(true)] out IMethodSymbol? splicingMethod
    ) {
        splicingMethod = default;

        // Is this a splicing method?
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
            return false;
        if(methodSymbol.ReducedFrom is null)
            return false;
        if(!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _context.TypeSymbols.SplicingOperations))
            return false;

        // Is the method accessed from the active context identifier?
        if(node.Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: var identifier }})
            return false;
        if(!_contextIdentifier.Contains(identifier))
            return false;

        splicingMethod = methodSymbol;
        return true;
    }

    private bool IsContextDataAccess(MemberAccessExpressionSyntax node) {
        // Is this the data property?
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IPropertySymbol propertySymbol)
            return false;
        if(propertySymbol is not { Name: "Data", ContainingType.IsGenericType: true })
            return false;
        if(!SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingType.ConstructUnboundGenericType(), _context.TypeSymbols.IInterpolationContext1))
            return false;

        // Is the property accessed from the active context identifier?
        if(node.Expression is not IdentifierNameSyntax { Identifier.ValueText: var identifier })
            return false;
        if(!_contextIdentifier.Contains(identifier))
            return false;

        return true;
    }
}
