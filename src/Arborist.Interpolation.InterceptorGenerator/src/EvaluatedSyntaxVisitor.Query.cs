using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitor {
    public override InterpolatedTree VisitQueryExpression(QueryExpressionSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node.FromClause);
        var inputTree = CreateQueryInput(node.FromClause, node.FromClause.Expression, qci.CastInfo.Symbol);
        _queryContext = _queryContext.BindQuery(node.FromClause.Identifier.Text, inputTree, node.Body);
        return Visit(node.Body);
    }

    public override InterpolatedTree VisitQueryBody(QueryBodySyntax node) {
        foreach(var clause in node.Clauses)
            _queryContext.Tree = Visit(clause);

        var queryTree = Visit(node.SelectOrGroup);
        _queryContext = _queryContext.Restore();

        if(node.Continuation is null)
            return queryTree;

        // N.B. Restore handled by the subsequent call to VisitQueryBody
        _queryContext = _queryContext.BindQuery(node.Continuation.Identifier.Text, queryTree, node.Continuation.Body);
        return VisitQueryBody(node.Continuation.Body);
    }

    public override InterpolatedTree VisitFromClause(FromClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        return CreateQueryOperationCall(node, method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                CreateQueryInput(node, node.Expression, qci.CastInfo.Symbol)
            ),
            CreateFromResultTree(node)
        ]);
    }

    private InterpolatedTree CreateFromResultTree(FromClauseSyntax node) {
        var joinedIdentifier = node.Identifier.Text;
        var joinedParameter = InterpolatedTree.Verbatim(joinedIdentifier);
        _queryContext.BindJoined(node.Identifier.Text);

        // As an optimization, if the final clause preceding the select has a result projection,
        // the final output projection occurs here instead of in a trailing Select.
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.LastOrDefault())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        )
            return InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier), joinedParameter],
                Visit(selectClause.Expression)
            );

        var resultTree = InterpolatedTree.Lambda(
            [InterpolatedTree.Verbatim(_queryContext.InputIdentifier), joinedParameter],
            InterpolatedTree.AnonymousClass([
                InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                joinedParameter
            ])
        );

        _queryContext.RebindInput();
        return resultTree;
    }

    public override InterpolatedTree VisitGroupClause(GroupClauseSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        return CreateQueryOperationCall(node, method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                Visit(node.ByExpression)
            ),
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                Visit(node.GroupExpression)
            )
        ]);
    }

    public override InterpolatedTree VisitJoinClause(JoinClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        var inTree = CreateQueryInput(node, node.InExpression, qci.CastInfo.Symbol);

        var leftTree = InterpolatedTree.Lambda(
            [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            Visit(node.LeftExpression)
        );

        _queryContext.BindJoined(node.Identifier.Text);
        var rightTree = InterpolatedTree.Lambda(
            [InterpolatedTree.Verbatim(node.Identifier.Text)],
            Visit(node.RightExpression)
        );

        var resultProjectionTree = CreateJoinResultTree(node);

        _queryContext.RebindInput();

        return CreateQueryOperationCall(node, method, [
            _queryContext.Tree,
            inTree,
            leftTree,
            rightTree,
            resultProjectionTree
        ]);
    }

    private InterpolatedTree CreateJoinRightTree(JoinClauseSyntax node) {
        // The input identifier into the right expression is discarded in the event that the
        // clause is a GroupJoin, so we defer binding the joined identifier until we handle
        // the result expression tree.
        var snapshot = _evaluableIdentifiers;
        try {
            var rightIdentifier = node.Identifier.Text;
            var rightParameter = InterpolatedTree.Verbatim(rightIdentifier);
            _evaluableIdentifiers = _evaluableIdentifiers.SetItem(rightIdentifier, rightParameter);

            return InterpolatedTree.Lambda([rightParameter], Visit(node.RightExpression));
        } finally {
            _evaluableIdentifiers = snapshot;
        }
    }

    private InterpolatedTree CreateJoinResultTree(JoinClauseSyntax node) {
        var resultIdentifier = node.Into?.Identifier.Text ?? node.Identifier.Text;
        _queryContext.BindJoined(resultIdentifier);

        // As an optimization, if the final clause preceding the select has a result projection,
        // the final output projection occurs here instead of in a trailing Select.
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.Last())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        )
            return InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier), InterpolatedTree.Verbatim(resultIdentifier)],
                Visit(selectClause.Expression)
            );

        return InterpolatedTree.Lambda(
            [InterpolatedTree.Verbatim(_queryContext.InputIdentifier), InterpolatedTree.Verbatim(resultIdentifier)],
            InterpolatedTree.AnonymousClass([
                InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                InterpolatedTree.Verbatim(resultIdentifier)
            ])
        );
    }

    public override InterpolatedTree VisitLetClause(LetClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        // query.Select(x => new { x, id = <expr> })
        var resultTree = CreateQueryOperationCall(node, method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                InterpolatedTree.AnonymousClass([
                    InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                    InterpolatedTree.Interpolate($"{node.Identifier.Text} = {Visit(node.Expression)}")
                ])
            )
        ]);

        _queryContext.BindJoined(node.Identifier.Text);
        _queryContext.RebindInput();
        return resultTree;
    }

    public override InterpolatedTree VisitOrderByClause(OrderByClauseSyntax node) {
        foreach(var ordering in node.Orderings)
            _queryContext.Tree = VisitOrdering(ordering);

        return _queryContext.Tree;
    }

    public override InterpolatedTree VisitOrdering(OrderingSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        return CreateQueryOperationCall(node, method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                Visit(node.Expression)
            )
        ]);
    }

    public override InterpolatedTree VisitSelectClause(SelectClauseSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _queryContext.Tree;
        if(!TypeSymbolHelpers.IsAccessible(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        return CreateQueryOperationCall(node, method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                Visit(node.Expression)
            )
        ]);
    }

    public override InterpolatedTree VisitWhereClause(WhereClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(method))
            return _context.Diagnostics.InaccessibleSymbol(method, node);

        return CreateQueryOperationCall(node, method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                Visit(node.Condition)
            )
        ]);
    }

    private InterpolatedTree CreateQueryOperationCall(SyntaxNode clause, IMethodSymbol method, IReadOnlyList<InterpolatedTree> arguments) {
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

    private InterpolatedTree CreateQueryInput(SyntaxNode clause, ExpressionSyntax inputNode, ISymbol? castSymbol) {
        var inputTree = Visit(inputNode);

        if(castSymbol is null)
            return inputTree;
        if(castSymbol is not IMethodSymbol castMethod)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(clause);
        if(!TypeSymbolHelpers.IsAccessible(castMethod))
            return _context.Diagnostics.InaccessibleSymbol(castMethod, clause);
        if(castMethod is not { IsGenericMethod: true, TypeArguments.Length: 1 })
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(clause);

        var castTypeName = _builder.CreateTypeName(castMethod.TypeArguments[0], clause);

        if(castMethod is { ReducedFrom: {} })
            return InterpolatedTree.Call(
                InterpolatedTree.Interpolate($"{_builder.CreateTypeName(castMethod.ContainingType, clause)}.{castMethod.Name}<{castTypeName}>"),
                [inputTree]
            );

        return InterpolatedTree.Call(
            InterpolatedTree.Interpolate($"{inputTree}.{castMethod.Name}<{castTypeName}>"),
            []
        );
    }

    private sealed class QueryContext {
        public static QueryContext Create(EvaluatedSyntaxVisitor visitor) =>
            new QueryContext(default, visitor, "", InterpolatedTree.Unsupported, default!);

        private QueryContext(
            QueryContext? parentContext,
            EvaluatedSyntaxVisitor visitor,
            string inputIdentifier,
            InterpolatedTree tree,
            QueryBodySyntax queryBody
        ) {
            _parentContext = parentContext;
            _visitor = visitor;
            _evaluableIdentifiersSnapshot = visitor._evaluableIdentifiers;
            InputIdentifier = inputIdentifier;
            Tree = tree;
            QueryBody = queryBody;
            _bindings = ImmutableDictionary<string, InterpolatedTree>.Empty.WithComparers(IdentifierEqualityComparer.Instance);
            BindJoined(inputIdentifier);
        }

        private readonly QueryContext? _parentContext;
        private readonly EvaluatedSyntaxVisitor _visitor;
        private readonly ImmutableDictionary<string, InterpolatedTree> _evaluableIdentifiersSnapshot;
        public string InputIdentifier { get; private set; }
        public InterpolatedTree Tree { get; set; }
        public QueryBodySyntax QueryBody { get; }
        private ImmutableDictionary<string, InterpolatedTree> _bindings;

        public QueryContext BindQuery(
            string inputIdentifier,
            InterpolatedTree tree,
            QueryBodySyntax queryBody
        ) =>
            new QueryContext(this, _visitor, inputIdentifier, tree, queryBody);

        public QueryContext Restore() {
            if(_parentContext is null)
                throw new InvalidOperationException();

            _visitor._evaluableIdentifiers = _evaluableIdentifiersSnapshot;
            return _parentContext;
        }

        public void BindJoined(string identifier) {
            var bound = InterpolatedTree.Verbatim(identifier);
            BindOne(identifier, bound);
        }

        public void RebindInput() {
            InputIdentifier = _visitor._builder.CreateIdentifier();
            foreach(var binding in _bindings) {
                var rebound = InterpolatedTree.Member(InterpolatedTree.Verbatim(InputIdentifier), binding.Value);
                BindOne(binding.Key, rebound);
            }
        }

        private void BindOne(string identifier, InterpolatedTree bound) {
            _bindings = _bindings.SetItem(identifier, bound);
            _visitor._evaluableIdentifiers = _visitor._evaluableIdentifiers.SetItem(identifier, bound);
        }
    }
}
