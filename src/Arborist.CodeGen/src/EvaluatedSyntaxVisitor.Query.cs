using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public partial class EvaluatedSyntaxVisitor {
    public override InterpolatedTree VisitQueryExpression(QueryExpressionSyntax node) {
        var tree = Visit(node.FromClause.Expression);
        _queryContext = _queryContext.BindQuery(node.FromClause.Identifier.Text, tree, node.Body);
        return Visit(node.Body);
    }

    public override InterpolatedTree VisitQueryBody(QueryBodySyntax node) {
        foreach(var clause in node.Clauses)
            _queryContext.Tree = Visit(clause);

        var queryTree = Visit(node.SelectOrGroup);
        _queryContext = _queryContext.Restore();

        if(node.Continuation is not null) {
            _queryContext = _queryContext.BindQuery(node.Continuation.Identifier.Text, queryTree, node.Continuation.Body);
            queryTree = VisitQueryBody(node.Continuation.Body);
            // N.B. release handled by visit to continuation body
        }

        return queryTree;
    }

    public override InterpolatedTree VisitFromClause(FromClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        var expressionTree = qci.CastInfo.Symbol switch {
            IMethodSymbol castMethod => CreateQueryCastCall(node.Type!, castMethod, Visit(node.Expression)),
            _ => Visit(node.Expression)
        };

        _queryContext.BindInput(node.Identifier.Text);

        switch(method.Name) {
            case "Select":
                return CreateQueryOperationCall(method, [
                    _queryContext.Tree,
                    InterpolatedTree.Lambda([InterpolatedTree.Verbatim(_queryContext.InputIdentifier)], expressionTree)
                ]);

            case "SelectMany":
                var resultTree = CreateQueryOperationCall(method, [
                    _queryContext.Tree,
                    InterpolatedTree.Lambda([InterpolatedTree.Verbatim(_queryContext.InputIdentifier)], expressionTree),
                    CreateSelectManyResultProjection(node)
                ]);

                _queryContext.RebindInputs();
                return resultTree;

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        }
    }

    private InterpolatedTree CreateSelectManyResultProjection(FromClauseSyntax node) {
        // As an optimization, if the final clause preceding the select has a result projection,
        // the final output projection occurs here instead of in a trailing Select.
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.Last())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        ) {
            return InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier), InterpolatedTree.Verbatim(node.Identifier.Text)],
                Visit(selectClause.Expression)
            );
        }

        return InterpolatedTree.Lambda(
            [InterpolatedTree.Verbatim(_queryContext.InputIdentifier), InterpolatedTree.Verbatim(node.Identifier.Text)],
            InterpolatedTree.AnonymousClass([
                InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                InterpolatedTree.Verbatim(node.Identifier.Text)
            ])
        );
    }

    public override InterpolatedTree VisitGroupClause(GroupClauseSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        return CreateQueryOperationCall(method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                Visit(node.ByExpression)
            )
        ]);
    }

    public override InterpolatedTree VisitJoinClause(JoinClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        var inTree = InterpolatedTree.Lambda(
            [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            qci.CastInfo.Symbol switch {
                IMethodSymbol castMethod => CreateQueryCastCall(node.Type!, castMethod, Visit(node.InExpression)),
                _ => Visit(node.InExpression)
            }
        );

        var leftTree = InterpolatedTree.Lambda(
            [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
            Visit(node.LeftExpression)
        );

        _queryContext.BindInput(node.Identifier.Text);
        var rightTree = InterpolatedTree.Lambda(
            [InterpolatedTree.Verbatim(node.Identifier.Text)],
            Visit(node.RightExpression)
        );

        var resultProjectionTree = CreateJoinResultProjection(node);

        _queryContext.RebindInputs();

        return CreateQueryOperationCall(method, [
            _queryContext.Tree,
            inTree,
            leftTree,
            rightTree,
            resultProjectionTree
        ]);
    }

    private InterpolatedTree CreateJoinResultProjection(JoinClauseSyntax node) {
        var resultIdentifier = node.Into?.Identifier.Text ?? node.Identifier.Text;
        _queryContext.BindInput(resultIdentifier);

        // As an optimization, if the final clause preceding the select has a result projection,
        // the final output projection occurs here instead of in a trailing Select.
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.Last())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        ) {
            return InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier), InterpolatedTree.Verbatim(resultIdentifier)],
                Visit(selectClause.Expression)
            );
        }

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

        // query.Select(x => new { x, id = <expr> })
        var resultTree = CreateQueryOperationCall(method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                InterpolatedTree.AnonymousClass([
                    InterpolatedTree.Verbatim(_queryContext.InputIdentifier),
                    InterpolatedTree.Concat(
                        InterpolatedTree.Verbatim(node.Identifier.Text),
                        InterpolatedTree.Verbatim(" = "),
                        Visit(node.Expression)
                    )
                ])
            )
        ]);

        _queryContext.BindInput(node.Identifier.Text);
        _queryContext.RebindInputs();
        return resultTree;
    }

    public override InterpolatedTree VisitOrderByClause(OrderByClauseSyntax node) {
        foreach(var ordering in node.Orderings)
            _queryContext.Tree = Visit(ordering);

        return _queryContext.Tree;
    }

    public override InterpolatedTree VisitOrdering(OrderingSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        return CreateQueryOperationCall(method, [
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

        return CreateQueryOperationCall(method, [
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

        return CreateQueryOperationCall(method, [
            _queryContext.Tree,
            InterpolatedTree.Lambda(
                [InterpolatedTree.Verbatim(_queryContext.InputIdentifier)],
                Visit(node.Condition)
            )
        ]);
    }

    private InterpolatedTree CreateQueryOperationCall(IMethodSymbol method, IReadOnlyList<InterpolatedTree> arguments) {
        // Static extension method wherein we know that any type arguments are inferrable
        if(method is { ReducedFrom: { IsStatic: true } }) {
            if(!TypeSymbolHelpers.TryCreateTypeName(method.ContainingType, out var typeName))
                return _context.Diagnostics.UnsupportedType(method.ContainingType);

            return InterpolatedTree.StaticCall(
                InterpolatedTree.Verbatim($"{typeName}.{method.Name}"),
                arguments
            );
        }

        return InterpolatedTree.InstanceCall(
            arguments[0],
            InterpolatedTree.Verbatim(method.Name),
            [..arguments.Skip(1)]
        );
    }

    private InterpolatedTree CreateQueryCastCall(TypeSyntax node, IMethodSymbol method, InterpolatedTree body) {
        if(method is not { IsGenericMethod: true, TypeArguments.Length: 1 })
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.TryCreateTypeName(method.TypeArguments[0], out var castTypeName))
            return _context.Diagnostics.UnsupportedType(method.TypeArguments[0]);

        if(method is { ReducedFrom: { IsStatic: true } }) {
            if(!TypeSymbolHelpers.TryCreateTypeName(method.ContainingType, out var typeName))
                return _context.Diagnostics.UnsupportedType(method.ContainingType);

            return InterpolatedTree.StaticCall(
                InterpolatedTree.Verbatim($"{typeName}.{method.Name}<{castTypeName}>"),
                [body]
            );
        }

        return InterpolatedTree.InstanceCall(
            body,
            InterpolatedTree.Verbatim($"{method.Name}<{castTypeName}>"),
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
            _evaluableIdentifiersSnapshot = visitor._evaluableParameters;
            InputIdentifier = inputIdentifier;
            Tree = tree;
            QueryBody = queryBody;
            Bindings = ImmutableDictionary<string, InterpolatedTree>.Empty.WithComparers(IdentifierEqualityComparer.Instance);
            BindInput(inputIdentifier);
        }

        private readonly QueryContext? _parentContext;
        private readonly EvaluatedSyntaxVisitor _visitor;
        private readonly ImmutableDictionary<string, InterpolatedTree> _evaluableIdentifiersSnapshot;
        public string InputIdentifier { get; private set; }
        public InterpolatedTree Tree { get; set; }
        public QueryBodySyntax QueryBody { get; }
        public ImmutableDictionary<string, InterpolatedTree> Bindings { get; private set; }

        public QueryContext BindQuery(string inputIdentifier, InterpolatedTree tree, QueryBodySyntax queryBody) =>
            new QueryContext(this, _visitor, inputIdentifier, tree, queryBody);

        public QueryContext Restore() {
            if(_parentContext is null)
                throw new InvalidOperationException();

            _visitor._evaluableParameters = _evaluableIdentifiersSnapshot;
            return _parentContext;
        }

        public void BindInput(string identifier) {
            var bound = InterpolatedTree.Verbatim(identifier);
            BindOne(identifier, bound);
        }

        public void RebindInputs() {
            InputIdentifier = _visitor._builder.CreateIdentifier();
            foreach(var binding in Bindings) {
                var rebound = InterpolatedTree.Member(InterpolatedTree.Verbatim(InputIdentifier), binding.Value);
                BindOne(binding.Key, rebound);
            }
        }

        private void BindOne(string identifier, InterpolatedTree bound) {
            Bindings = Bindings.SetItem(identifier, bound);
            _visitor._evaluableParameters = _visitor._evaluableParameters.SetItem(identifier, bound);
        }
    }
}
