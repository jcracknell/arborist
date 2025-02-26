using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitor {
    public override InterpolatedTree VisitQueryExpression(QueryExpressionSyntax node) =>
        VisitQueryBody(node.Body);

    public override InterpolatedTree VisitQueryBody(QueryBodySyntax node) {
        // Drill all the way down to the last query continuation (the outermost call)
        if(node.Continuation is not null)
            return VisitQueryBody(node.Continuation.Body);

        // Process the nodes from outermost to innermost in a traversal managed by the query context
        _queryContext = _queryContext.BeginQuery(node);
        var result = _queryContext.VisitNext();
        _queryContext = _queryContext.Restore();

        return result;
    }

    public override InterpolatedTree VisitFromClause(FromClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);

        // If there is no method associated with the operation, then this is the initial from clause
        if(qci.OperationInfo.Symbol is not IMethodSymbol method) {
            _queryContext.BindJoined(node.Identifier.ValueText);
            return CreateQueryInput(node, node.Expression, qci.CastInfo.Symbol);
        }

        if(!SymbolHelpers.IsAccessibleFromInterceptor(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());

        var selectorTree = CreateQueryLambda(
            method, 1,
            argumentTrees: [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            bodyFactory: () => CreateQueryInput(node, node.Expression, qci.CastInfo.Symbol)
        );

        // Register the identifier defined by this clause for consumption by subsequenc
        _queryContext.BindJoined(node.Identifier.ValueText);

        var projectionTree = CreateFromProjection(node, method);

        _queryContext.RebindInput();
        return CreateQueryCall(node, method, [inputTree, selectorTree, projectionTree]);
    }

    private InterpolatedTree CreateFromProjection(FromClauseSyntax node, IMethodSymbol method) {
        var inputParameter = InterpolatedTree.Verbatim(_queryContext.InputIdentifier);
        var joinedParameter = InterpolatedTree.Verbatim(node.Identifier.Text);

        // As an optimization, if the final clause preceding the select has a result projection,
        // the final output projection occurs here instead of in a trailing Select.
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.LastOrDefault())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        )
            return CreateQueryLambda(
                method, 2,
                argumentTrees: [inputParameter, joinedParameter],
                bodyFactory: () => Visit(selectClause.Expression)
            );

        // The default projection does not appear in code and does not require binding
        return CreateQueryLambda(
            method, 2,
            argumentTrees: [inputParameter, joinedParameter],
            bodyFactory: () => InterpolatedTree.AnonymousClass([inputParameter, joinedParameter])
        );
    }

    public override InterpolatedTree VisitGroupClause(GroupClauseSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!SymbolHelpers.IsAccessibleFromInterceptor(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());

        var keySelectorTree = CreateQueryLambda(
            method, 1,
            argumentTrees: [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            bodyFactory: () => Visit(node.ByExpression)
        );

        // If the element selector is the identity function, the two parameter overload is used
        if(SymbolHelpers.GetParameterCount(method) == 2)
            return CreateQueryCall(node, method, [inputTree, keySelectorTree]);

        var elementSelectorTree = CreateQueryLambda(
            method, 2,
            argumentTrees: [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            bodyFactory: () => Visit(node.GroupExpression)
        );

        return CreateQueryCall(node, method, [inputTree, keySelectorTree, elementSelectorTree]);
    }

    public override InterpolatedTree VisitJoinClause(JoinClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!SymbolHelpers.IsAccessibleFromInterceptor(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());
        var selectorTree = CurrentExpr.BindCallArg(method, 1).WithValue(CreateQueryInput(node, node.InExpression, qci.CastInfo.Symbol));

        var leftSelectorTree = CreateQueryLambda(
            method, 2,
            argumentTrees: [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            bodyFactory: () => Visit(node.LeftExpression)
        );

        _queryContext.BindJoined(node.Identifier.ValueText);
        var rightSelectorTree = CreateQueryLambda(
            method, 3,
            argumentTrees: [InterpolatedTree.Verbatim(node.Identifier.ValueText)],
            bodyFactory: () => Visit(node.RightExpression)
        );

        var projectionTree = CreateJoinProjectionTree(node, method);

        _queryContext.RebindInput();

        return CreateQueryCall(node, method, [
            inputTree,
            selectorTree,
            leftSelectorTree,
            rightSelectorTree,
            projectionTree
        ]);
    }

    private InterpolatedTree CreateJoinProjectionTree(JoinClauseSyntax node, IMethodSymbol method) {
        var resultIdentifier = node.Into?.Identifier.Text ?? node.Identifier.Text;
        _queryContext.BindJoined(resultIdentifier);

        // As an optimization, if the final clause preceding the select has a result projection,
        // the final output projection occurs here instead of in a trailing Select.
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.Last())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        )
            return CreateQueryLambda(
                method, 4,
                argumentTrees: [
                    InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                    InterpolatedTree.Verbatim(resultIdentifier)
                ],
                bodyFactory: () => Visit(selectClause.Expression)
            );

        // The default projection does not appear in code and does not require binding
        return CreateQueryLambda(
            method, 4,
            argumentTrees: [
                InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                InterpolatedTree.Verbatim(resultIdentifier)
            ],
            bodyFactory: () => InterpolatedTree.AnonymousClass([
                InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                InterpolatedTree.Verbatim(resultIdentifier)
            ])
        );
    }

    public override InterpolatedTree VisitLetClause(LetClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!SymbolHelpers.IsAccessibleFromInterceptor(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());

        var projectionTree = CreateQueryLambda(
            method, 1,
            argumentTrees: [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            bodyFactory: () => {
                CurrentExpr.SetType(typeof(NewExpression));
                return InterpolatedTree.AnonymousClass([
                    InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                    CurrentExpr.Bind($"{nameof(NewExpression.Arguments)}[1]")
                    .WithValue(InterpolatedTree.Interpolate($"{node.Identifier.ValueText} = {Visit(node.Expression)}"))
                ]);
            }
        );

        _queryContext.BindJoined(node.Identifier.Text);
        _queryContext.RebindInput();

        return CreateQueryCall(node, method, [inputTree, projectionTree]);
    }

    public override InterpolatedTree VisitOrderByClause(OrderByClauseSyntax node) =>
        VisitOrderByOrdering(node, node.Orderings.Count - 1);

    private InterpolatedTree VisitOrderByOrdering(OrderByClauseSyntax orderBy, int index) {
        var node = orderBy.Orderings[index];
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(index switch {
            0 => _queryContext.VisitNext(),
            _ => VisitOrderByOrdering(orderBy, index - 1)
        });

        var selectorTree = CreateQueryLambda(
            method, 1,
            argumentTrees: [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            bodyFactory: () => Visit(node.Expression)
        );

        return CreateQueryCall(node, method, [inputTree, selectorTree]);
    }

    public override InterpolatedTree VisitSelectClause(SelectClauseSyntax node) {
        // If there is no method associated with the select clause, the final result projection
        // is handled by the last query clause
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _queryContext.VisitNext();

        if(!SymbolHelpers.IsAccessibleFromInterceptor(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());
        var projectionTree = CreateQueryLambda(
            method, 1,
            argumentTrees: [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            bodyFactory: () => Visit(node.Expression)
        );

        return CreateQueryCall(node, method, [inputTree, projectionTree]);
    }

    public override InterpolatedTree VisitWhereClause(WhereClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!SymbolHelpers.IsAccessibleFromInterceptor(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());
        var predicateTree = CreateQueryLambda(
            method, 1,
            argumentTrees: [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            bodyFactory: () => Visit(node.Condition)
        );

        return CreateQueryCall(node, method, [inputTree, predicateTree]);
    }

    private InterpolatedTree CreateQueryCall(SyntaxNode clause, IMethodSymbol method, IReadOnlyList<InterpolatedTree> arguments) {
        CurrentExpr.SetType(typeof(MethodCallExpression));
        // Static extension method wherein we know that any type arguments are inferrable (because
        // it was implicitly called by query syntax)
        if(method is { ReducedFrom: {} })
            return InterpolatedTree.Call(
                InterpolatedTree.Interpolate($"{_builder.CreateTypeName(method.ContainingType, clause)}.{method.Name}"),
                arguments
            );

        return InterpolatedTree.Call(
            InterpolatedTree.Interpolate($"{arguments[0]}.{method.Name}"),
            [..arguments.Skip(1)]
        );
    }

    // N.B. passing a delegate factory here appears to be necessitated by the case of the let clause,
    // which has a surprisingly complex body
    private InterpolatedTree CreateQueryLambda(
        IMethodSymbol method,
        int index,
        IReadOnlyList<InterpolatedTree> argumentTrees,
        Func<InterpolatedTree> bodyFactory
    ) {
        switch(SymbolHelpers.GetParameterType(method, index)) {
            case INamedTypeSymbol { IsGenericType: true } namedType
                when SymbolEqualityComparer.Default.Equals(namedType.ConstructUnboundGenericType(), _context.TypeSymbols.Expression1):
                return CurrentExpr.BindCallArg(typeof(UnaryExpression), method, index).WithValue(
                    CurrentExpr.Bind(typeof(LambdaExpression), $"{nameof(UnaryExpression.Operand)}")
                    .WithValue(InterpolatedTree.Lambda(
                        argumentTrees,
                        CurrentExpr.Bind($"{nameof(LambdaExpression.Body)}").WithValue(bodyFactory())
                    ))
                );

            default:
                return CurrentExpr.BindCallArg(typeof(LambdaExpression), method, index)
                .WithValue(InterpolatedTree.Lambda(
                    argumentTrees,
                    CurrentExpr.Bind($"{nameof(LambdaExpression.Body)}").WithValue(bodyFactory())
                ));
        }
    }

    private InterpolatedTree CreateQueryInput(SyntaxNode clause, ExpressionSyntax inputNode, ISymbol? castMethodSymbol) {
        if(castMethodSymbol is null)
            return Visit(inputNode);
        if(castMethodSymbol is not IMethodSymbol castMethod)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(clause);
        if(!SymbolHelpers.IsAccessibleFromInterceptor(castMethod))
            return _context.Diagnostics.InaccessibleSymbol(castMethod, clause);
        if(castMethod is not { IsGenericMethod: true, TypeArguments.Length: 1 })
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(clause);

        CurrentExpr.SetType(typeof(MethodCallExpression));
        var castTypeName = _builder.CreateTypeName(castMethod.TypeArguments[0], clause);
        var inputTree = CurrentExpr.BindCallArg(castMethod, 0).WithValue(Visit(inputNode));

        switch(castMethod) {
            case { ReducedFrom: {} }:
                var containingTypeName = _builder.CreateTypeName(castMethod.ContainingType, clause);
                return InterpolatedTree.Call(
                    InterpolatedTree.Interpolate($"{containingTypeName}.{castMethod.Name}<{castTypeName}>"),
                    [inputTree]
                );

            default:
                return InterpolatedTree.Call(
                    InterpolatedTree.Interpolate($"{inputTree}.{castMethod.Name}<{castTypeName}>"),
                    []
                );
        }
    }

    private sealed class QueryContext {
        public static QueryContext Create(EvaluatedSyntaxVisitor visitor) =>
            new QueryContext(default, visitor, default!);

        private QueryContext(
            QueryContext? parentContext,
            EvaluatedSyntaxVisitor visitor,
            QueryBodySyntax? queryBody
        ) {
            _parentContext = parentContext;
            _visitor = visitor;
            _evaluableIdentifiersSnapshot = visitor._evaluableIdentifiers;
            InputIdentifier = default!;
            QueryBody = queryBody!;
            _clauseIndex = queryBody?.Clauses.Count ?? 0;
            _bindings = ImmutableDictionary<string, InterpolatedTree>.Empty.WithComparers(IdentifierEqualityComparer.Instance);
        }

        private readonly QueryContext? _parentContext;
        private readonly EvaluatedSyntaxVisitor _visitor;
        private readonly ImmutableDictionary<string, InterpolatedTree> _evaluableIdentifiersSnapshot;
        public string InputIdentifier { get; private set; }
        public QueryBodySyntax QueryBody { get; private set; }
        private int _clauseIndex;
        private ImmutableDictionary<string, InterpolatedTree> _bindings;

        public QueryContext BeginQuery(QueryBodySyntax queryBody) =>
            new QueryContext(this, _visitor, queryBody);

        public QueryContext Restore() {
            if(_parentContext is null)
                throw new InvalidOperationException();

            _visitor._evaluableIdentifiers = _evaluableIdentifiersSnapshot;
            return _parentContext;
        }

        public void BindJoined(string identifier) {
            InputIdentifier ??= identifier;
            var bound = InterpolatedTree.Verbatim(identifier);
            BindOne(identifier, bound);
        }

        public void RebindInput() {
            InputIdentifier = _visitor._builder.CreateIdentifier();
            // Rebind all of our existing bindings as properties of the input identifier
            foreach(var binding in _bindings) {
                var rebound = InterpolatedTree.Member(InterpolatedTree.Verbatim(InputIdentifier), binding.Value);
                BindOne(binding.Key, rebound);
            }
        }

        private void BindOne(string identifier, InterpolatedTree bound) {
            _bindings = _bindings.SetItem(identifier, bound);
            _visitor._evaluableIdentifiers = _visitor._evaluableIdentifiers.SetItem(identifier, bound);
        }

        public InterpolatedTree VisitNext() {
            var clauseIndex = _clauseIndex;
            _clauseIndex -= 1;

            if(clauseIndex == QueryBody.Clauses.Count)
                return _visitor.Visit(QueryBody.SelectOrGroup);

            if(clauseIndex == -1) switch(QueryBody.Parent) {
                case QueryExpressionSyntax qe:
                    return _visitor.Visit(qe.FromClause);

                case QueryContinuationSyntax qc:
                    QueryBody = (QueryBodySyntax)qc.Parent!;
                    _clauseIndex = QueryBody.Clauses.Count;

                    // Take a snapshot of the identifiers defined by the current query body so that we can
                    // restore it when we have finished processing the preceding query body (identifiers are
                    // scoped to the query in which they are defined)
                    var snapshot = _visitor._evaluableIdentifiers;
                    var parentResultTree = VisitNext();
                    _visitor._evaluableIdentifiers = snapshot;

                    // At this point we are returning to the processing of the continuation (on the stack), so
                    // we need to bind the continuation identifier as the query input
                    InputIdentifier = null!;
                    BindJoined(qc.Identifier.ValueText);
                    return parentResultTree;
            }

            if(clauseIndex < 0)
                throw new InvalidOperationException();

            return _visitor.Visit(QueryBody.Clauses[clauseIndex]);
        }
    }
}
