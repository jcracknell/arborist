using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitor {
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
            // Register the identifier defined by this clause for consumption by subsequent clauses
            AddInterpolatedIdentifier(node.Identifier.ValueText);
            if(qci.CastInfo.Symbol is not IMethodSymbol castMethod)
                return Visit(node.Expression);

            // The clause maps to an expression of the form {expr}.Cast<T>()
            return CreateQueryCall(node, castMethod, [
                CurrentExpr.BindCallArg(castMethod, 0).WithValue(Visit(node.Expression))
            ]);
        }

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());
        var selectorTree = CreateFromSelector(node, method, qci.CastInfo.Symbol as IMethodSymbol);

        // Register the identifier defined by this clause for consumption by subsequent clauses
        AddInterpolatedIdentifier(node.Identifier.ValueText);

        var projectionTree = CreateFromProjection(node, method);

        return CreateQueryCall(node, method, [inputTree, selectorTree, projectionTree]);
    }

    private InterpolatedTree CreateFromSelector(FromClauseSyntax node, IMethodSymbol method, IMethodSymbol? castMethod) {
        if(castMethod is null)
            return CreateQueryLambda(method, 1, bodyFactory: () => Visit(node.Expression));

        // The selector has the form _ => {expr}.Cast<T>()
        return CurrentExpr.BindCallArg(typeof(LambdaExpression), method, 1)
        .WithValue(_builder.CreateExpression(nameof(Expression.Lambda), [
            CurrentExpr.Bind(typeof(MethodCallExpression), $"{nameof(LambdaExpression.Body)}")
            .WithValue(CreateQueryCall(node, castMethod, [
                CurrentExpr.BindCallArg(castMethod, 0).WithValue(Visit(node.Expression))
            ])),
            CurrentExpr.BindValue($"{nameof(LambdaExpression.Parameters)}")
        ]));
    }

    private InterpolatedTree CreateFromProjection(FromClauseSyntax node, IMethodSymbol method) {
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.LastOrDefault())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        )
            return CreateQueryLambda(method, 2, bodyFactory: () => Visit(selectClause.Expression));

        // If this is a join projection, we don't need to do anything as the expression is not present in code
        // and thus cannot contain interpolations.
        return CurrentExpr.BindCallArg(typeof(LambdaExpression), method, 2)
        .WithValue(CurrentExpr.Identifier);
    }

    public override InterpolatedTree VisitGroupClause(GroupClauseSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());
        var keySelectorTree = CreateQueryLambda(method, 1, bodyFactory: () => Visit(node.ByExpression));

        // If the element selector is the identity function, the two parameter overload is used
        if(SymbolHelpers.GetParameterCount(method) == 2)
            return CreateQueryCall(node, method, [inputTree, keySelectorTree]);

        var elementSelectorTree = CreateQueryLambda(method, 2, bodyFactory: () => Visit(node.GroupExpression));

        return CreateQueryCall(node, method, [inputTree, keySelectorTree, elementSelectorTree]);
    }

    public override InterpolatedTree VisitJoinClause(JoinClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());
        var selectorTree = CreateJoinSelector(node, method, qci.CastInfo.Symbol as IMethodSymbol);
        var outerKeySelectorTree = CreateQueryLambda(method, 2, bodyFactory: () => Visit(node.LeftExpression));
        var innerKeySelectorTree = CreateJoinInnerKeySelector(node, method);
        var resultProjectionTree = CreateJoinResultProjection(node, method);

        return CreateQueryCall(node, method, [
            inputTree,
            selectorTree,
            outerKeySelectorTree,
            innerKeySelectorTree,
            resultProjectionTree
        ]);
    }

    private InterpolatedTree CreateJoinSelector(JoinClauseSyntax node, IMethodSymbol method, IMethodSymbol? castMethod) {
        if(castMethod is null)
            return CurrentExpr.BindCallArg(method, 1).WithValue(Visit(node.InExpression));

        return CurrentExpr.BindCallArg(typeof(MethodCallExpression), method, 1)
        .WithValue(CreateQueryCall(node, castMethod, [
            CurrentExpr.BindCallArg(castMethod, 0).WithValue(Visit(node.InExpression))
        ]));
    }

    private InterpolatedTree CreateJoinInnerKeySelector(JoinClauseSyntax node, IMethodSymbol method) {
        // The input identifier into the inner key selector is discarded in the event that the clause
        // is a GroupJoin, so we create the binding manually here and bind the joined identifier when
        // processing the result projection
        using var snapshot = CreateIdentifiersSnapshot();

        AddInterpolatedIdentifier(node.Identifier.ValueText);
        return CreateQueryLambda(method, 3, bodyFactory: () => Visit(node.RightExpression));
    }

    private InterpolatedTree CreateJoinResultProjection(JoinClauseSyntax node, IMethodSymbol method) {
        // Join and GroupJoin are actually more or less identical - the only difference between the two
        // is the joined identifier which is specified by the into clause
        var joinedIdentifier = node.Into?.Identifier ?? node.Identifier;
        AddInterpolatedIdentifier(joinedIdentifier.ValueText);

        // As an optimization, if the final clause preceding the select has a result projection,
        // then the final output projection occurs here instead of in a trailing select
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.LastOrDefault())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        )
            return CreateQueryLambda(method, 4, bodyFactory: () => Visit(selectClause.Expression));

        // Otherwise the result projection does not appear in code and does not need to be interpolated
        return CurrentExpr.BindCallArg(typeof(LambdaExpression), method, 4)
        .WithValue(CurrentExpr.Identifier);
    }

    public override InterpolatedTree VisitLetClause(LetClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());

        var projectionTree = CreateQueryLambda(method, 1, bodyFactory: () => {
            CurrentExpr.SetType(typeof(NewExpression));
            return _builder.CreateExpression(nameof(Expression.New), [
                CurrentExpr.BindValue($"{nameof(NewExpression.Constructor)}!"),
                _builder.CreateExpressionArray([
                    CurrentExpr.BindValue($"{nameof(NewExpression.Arguments)}[0]"),
                    CurrentExpr.Bind($"{nameof(NewExpression.Arguments)}[1]")
                    .WithValue(Visit(node.Expression))
                ]),
                CurrentExpr.BindValue($"{nameof(NewExpression.Members)}"),
            ]);
        });

        // Register the identifier defined by this clause for consumption by subsequent clauses
        AddInterpolatedIdentifier(node.Identifier.ValueText);

        return CreateQueryCall(node, method, [inputTree, projectionTree]);
    }

    public override InterpolatedTree VisitOrderByClause(OrderByClauseSyntax node) =>
        VisitOrderByOrdering(node, node.Orderings.Count - 1);

    private InterpolatedTree VisitOrderByOrdering(OrderByClauseSyntax orderBy, int index) {
        var node = orderBy.Orderings[index];
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(index switch {
            0 => _queryContext.VisitNext(),
            _ => VisitOrderByOrdering(orderBy, index - 1)
        });

        var selectorTree = CreateQueryLambda(method, 1, bodyFactory: () => Visit(node.Expression));

        return CreateQueryCall(node, method, [inputTree, selectorTree]);
    }

    public override InterpolatedTree VisitSelectClause(SelectClauseSyntax node) {
        // If there is no method associated with the select clause, the final result projection
        // is handled by the last query clause
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _queryContext.VisitNext();

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());
        var projectionTree = CreateQueryLambda(method, 1, bodyFactory: () => Visit(node.Expression));

        return CreateQueryCall(node, method, [inputTree, projectionTree]);
    }

    public override InterpolatedTree VisitWhereClause(WhereClauseSyntax node) {
        if(_context.SemanticModel.GetQueryClauseInfo(node).OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputTree = CurrentExpr.BindCallArg(method, 0).WithValue(_queryContext.VisitNext());
        var predicateTree = CreateQueryLambda(method, 1, bodyFactory: () => Visit(node.Condition));

        return CreateQueryCall(node, method, [inputTree, predicateTree]);
    }

    // N.B. passing a delegate factory here appears to be necessitated by the case of the let clause,
    // which has a surprisingly complex body
    private InterpolatedTree CreateQueryLambda(
        IMethodSymbol method,
        int index,
        Func<InterpolatedTree> bodyFactory
    ) {
        switch(SymbolHelpers.GetParameterType(method, index)) {
            case INamedTypeSymbol { IsGenericType: true } namedType
                when SymbolEqualityComparer.Default.Equals(namedType.ConstructUnboundGenericType(), _context.TypeSymbols.Expression1):
                return CurrentExpr.BindCallArg(typeof(UnaryExpression), method, index)
                .WithValue(_builder.CreateExpression(nameof(Expression.Quote), [
                    CurrentExpr.Bind(typeof(LambdaExpression), $"{nameof(UnaryExpression.Operand)}")
                    .WithValue(_builder.CreateExpression(nameof(Expression.Lambda), [
                        CurrentExpr.BindValue($"{nameof(LambdaExpression.Type)}"),
                        CurrentExpr.Bind($"{nameof(LambdaExpression.Body)}").WithValue(bodyFactory()),
                        CurrentExpr.BindValue($"{nameof(LambdaExpression.Parameters)}")
                    ]))
                ]));

            default:
                return CurrentExpr.BindCallArg(typeof(LambdaExpression), method, index)
                .WithValue(_builder.CreateExpression(nameof(Expression.Lambda), [
                    CurrentExpr.BindValue($"{nameof(LambdaExpression.Type)}"),
                    CurrentExpr.Bind($"{nameof(LambdaExpression.Body)}").WithValue(bodyFactory()),
                    CurrentExpr.BindValue($"{nameof(LambdaExpression.Parameters)}")
                ]));
        }
    }

    private InterpolatedTree CreateQueryCall(SyntaxNode node, IMethodSymbol method, IReadOnlyList<InterpolatedTree> args) {
        CurrentExpr.SetType(typeof(MethodCallExpression));

        return method switch {
            { ReducedFrom: { } } => _builder.CreateExpression(nameof(Expression.Call), [
                _builder.CreateDefaultValue(_context.TypeSymbols.Expression.WithNullableAnnotation(NullableAnnotation.Annotated)),
                CurrentExpr.BindValue($"{nameof(MethodCallExpression.Method)}"),
                ..args
            ]),
            _ => _builder.CreateExpression(nameof(Expression.Call), [
                args[0],
                CurrentExpr.BindValue($"{nameof(MethodCallExpression.Method)}"),
                ..args.Skip(1)
            ])
        };
    }

    private sealed class QueryContext {
        public static QueryContext Create(InterpolatedSyntaxVisitor visitor) =>
            new(visitor, default, default);

        private QueryContext(
            InterpolatedSyntaxVisitor visitor,
            QueryContext? parentContext,
            QueryBodySyntax? queryBody
        ) {
            _visitor = visitor;
            _parentContext = parentContext;
            QueryBody = queryBody!;
            _clauseIndex = queryBody?.Clauses.Count ?? 0;
            _identifiersSnapshot = visitor.CreateIdentifiersSnapshot();
        }

        private readonly QueryContext? _parentContext;
        private readonly InterpolatedSyntaxVisitor _visitor;
        public QueryBodySyntax QueryBody { get; private set; }
        private int _clauseIndex;
        private readonly IdentifiersSnapshot _identifiersSnapshot;

        public QueryContext BeginQuery(QueryBodySyntax queryBody) =>
            new(_visitor, this, queryBody);

        public QueryContext Restore() {
            if(_parentContext is null)
                throw new InvalidOperationException();

            _identifiersSnapshot.Restore();
            return _parentContext;
        }

        public InterpolatedTree VisitNext() {
            var clauseIndex = _clauseIndex;
            _clauseIndex -= 1;

            if(clauseIndex == QueryBody.Clauses.Count) {
                // We are starting processing of the query body, so add the continuation identifier if
                // this is a query continuation
                if(QueryBody.Parent is QueryContinuationSyntax qc)
                    _visitor.AddInterpolatedIdentifier(qc.Identifier.ValueText);

                return _visitor.Visit(QueryBody.SelectOrGroup);
            }

            if(clauseIndex == -1) switch(QueryBody.Parent) {
                case QueryExpressionSyntax qe:
                    return _visitor.Visit(qe.FromClause);

                case QueryContinuationSyntax qc:
                    QueryBody = (QueryBodySyntax)qc.Parent!;
                    _clauseIndex = QueryBody.Clauses.Count;

                    // Take a snapshot of the identifiers defined by the current query body so that we can
                    // restore it when we have finished processing the preceding query body (identifiers are
                    // scoped to the query in which they are defined)
                    using(_visitor.CreateIdentifiersSnapshot()) {
                        _identifiersSnapshot.Restore();
                        return VisitNext();
                    }
            }

            if(clauseIndex < 0)
                throw new InvalidOperationException();

            return _visitor.Visit(QueryBody.Clauses[clauseIndex]);
        }
    }
}
