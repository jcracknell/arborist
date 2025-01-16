using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public partial class InterpolatedSyntaxVisitor {
    private IReadOnlyList<ITypeSymbol> GetQueryMethodArgumentTypes(IMethodSymbol method) =>
        method.Parameters.Select(static p => p.Type).ToList();

    private bool TryGetQueryDelegateTypeArgs(ITypeSymbol type, out IReadOnlyList<ITypeSymbol> typeArgs) {
        typeArgs = default!;
        if(type is not INamedTypeSymbol { IsGenericType: true } generic)
            return false;
        if(SymbolEqualityComparer.Default.Equals(generic.ConstructUnboundGenericType(), _context.TypeSymbols.Expression1))
            return TryGetQueryDelegateTypeArgs(generic.TypeArguments[0], out typeArgs);

        typeArgs = generic.TypeArguments;
        return true;
    }

    public override InterpolatedTree VisitQueryExpression(QueryExpressionSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node.FromClause);
        var inputTree = CreateQueryInput(node.FromClause, node.FromClause.Expression, qci.CastInfo.Symbol);
        _queryContext = _queryContext.BindQuery(node.FromClause.Identifier.Text, inputTree, node.Body);
        return VisitQueryBody(node.Body);
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
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var argumentTypes = GetQueryMethodArgumentTypes(method);
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[0], out var expressionTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputParameter = _queryContext.BindInput(expressionTypeArgs[0]);

        return _queryContext.CreateQueryCall(method, [
            _queryContext.Tree,
            _builder.CreateExpression(nameof(Expression.Lambda), [
                CreateQueryInput(node, node.Expression, qci.CastInfo.Symbol),
                inputParameter
            ]),
            CreateFromResultTree(node, argumentTypes, inputParameter)
        ]);
    }

    private InterpolatedTree CreateFromResultTree(
        FromClauseSyntax node,
        IReadOnlyList<ITypeSymbol> argumentTypes,
        InterpolatedTree inputParameter
    ) {
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[1], out var resultProjectionTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var joinedType = resultProjectionTypeArgs[1];
        var joinedParameter = _builder.CreateParameter(joinedType, node.Identifier.Text);
        _queryContext.BindJoined(node.Identifier.Text, joinedParameter);

        // As an optimization, if the final clause preceding the select has a result projection,
        // the final output projection occurs here instead of in a trailing Select.
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.LastOrDefault())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        )
            return _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(selectClause.Expression),
                inputParameter,
                joinedParameter
            ]);

        var outputType = _queryContext.RebindInput(resultProjectionTypeArgs[2]);

        return _builder.CreateExpression(nameof(Expression.Lambda), [
            _builder.CreateAnonymousClassExpression(outputType, [
                inputParameter,
                joinedParameter
            ]),
            inputParameter,
            joinedParameter
        ]);
    }

    public override InterpolatedTree VisitGroupClause(GroupClauseSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var argumentTypes = GetQueryMethodArgumentTypes(method);
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[0], out var expressionTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputParameter = _queryContext.BindInput(expressionTypeArgs[0]);

        return _queryContext.CreateQueryCall(method, [
            _queryContext.Tree,
            _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.ByExpression),
                inputParameter
            ]),
            _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.GroupExpression),
                inputParameter
            ])
        ]);
    }

    public override InterpolatedTree VisitJoinClause(JoinClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var argumentTypes = GetQueryMethodArgumentTypes(method);
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[1], out var leftExpressionTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputParameter = _queryContext.BindInput(leftExpressionTypeArgs[0]);

        var inTree = CreateQueryInput(node, node.InExpression, qci.CastInfo.Symbol);

        var leftTree = _builder.CreateExpression(nameof(Expression.Lambda), [
            Visit(node.LeftExpression),
            inputParameter
        ]);

        var rightTree = CreateJoinRightTree(node, argumentTypes, inputParameter);
        var resultProjectionTree = CreateJoinResultTree(node, argumentTypes, inputParameter);

        return _queryContext.CreateQueryCall(method, [
            _queryContext.Tree,
            inTree,
            leftTree,
            rightTree,
            resultProjectionTree
        ]);
    }

    private InterpolatedTree CreateJoinRightTree(
        JoinClauseSyntax node,
        IReadOnlyList<ITypeSymbol> argumentTypes,
        InterpolatedTree inputParameter
    ) {
        // The input identifier into the right expression is discarded in the event that the
        // clause is a GroupJoin, so we defer binding the joined identifier until we handle
        // the result expression tree.
        var snapshot = _interpolatableIdentifiers;
        try {
            if(!TryGetQueryDelegateTypeArgs(argumentTypes[2], out var rightExpressionTypeArgs))
                return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

            var rightType = rightExpressionTypeArgs[0];
            var rightIdentifier = node.Identifier.Text;
            var rightParameter = _builder.CreateParameter(rightType, rightIdentifier);
            _interpolatableIdentifiers = _interpolatableIdentifiers.SetItem(rightIdentifier, rightParameter);

            return _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.RightExpression),
                rightParameter
            ]);
        } finally {
            _interpolatableIdentifiers = snapshot;
        }
    }

    private InterpolatedTree CreateJoinResultTree(
        JoinClauseSyntax node,
        IReadOnlyList<ITypeSymbol> argumentTypes,
        InterpolatedTree inputParameter
    ) {
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[3], out var resultTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        // Join and GroupJoin are actually more or less identical - the only difference between
        // the two is the result type and the joined identifier which is specified by the into
        // clause.
        var joinedType = resultTypeArgs[1];
        var joinedIdentifier = node.Into?.Identifier.Text ?? node.Identifier.Text;
        var joinedParameter = _builder.CreateParameter(joinedType, joinedIdentifier);
        _queryContext.BindJoined(joinedIdentifier, joinedParameter);

        // As an optimization, if the final clause preceding the select has a result projection,
        // the final output projection occurs here instead of in a trailing Select.
        if(
            ReferenceEquals(node, _queryContext.QueryBody.Clauses.LastOrDefault())
            && _queryContext.QueryBody.SelectOrGroup is SelectClauseSyntax selectClause
            && _context.SemanticModel.GetSymbolInfo(selectClause).Symbol is not IMethodSymbol
        )
            return _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(selectClause.Expression),
                inputParameter,
                joinedParameter
            ]);

        var outputType = _queryContext.RebindInput(resultTypeArgs[2]);

        return _builder.CreateExpression(nameof(Expression.Lambda), [
            _builder.CreateAnonymousClassExpression(outputType, [
                inputParameter,
                joinedParameter
            ]),
            inputParameter,
            joinedParameter
        ]);
    }

    public override InterpolatedTree VisitLetClause(LetClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var argumentTypes = GetQueryMethodArgumentTypes(method);
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[0], out var expressionTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        if(_context.SemanticModel.GetTypeInfo(node.Expression).Type is not {} joinedType)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputParameter = _queryContext.BindInput(expressionTypeArgs[0]);
        var innerTree = Visit(node.Expression);

        _queryContext.BindJoined(node.Identifier.Text, InterpolatedTree.Unsupported);
        var resultType = _queryContext.RebindInput(expressionTypeArgs[1]);

        return _queryContext.CreateQueryCall(method, [
            _queryContext.Tree,
            _builder.CreateExpression(nameof(Expression.Lambda), [
                _builder.CreateAnonymousClassExpression(resultType, [
                    inputParameter,
                    innerTree
                ]),
                inputParameter
            ])
        ]);
    }

    public override InterpolatedTree VisitOrderByClause(OrderByClauseSyntax node) {
        foreach(var ordering in node.Orderings)
            _queryContext.Tree = VisitOrdering(ordering);

        return _queryContext.Tree;
    }

    public override InterpolatedTree VisitOrdering(OrderingSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var argumentTypes = GetQueryMethodArgumentTypes(method);
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[0], out var expressionTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputParameter = _queryContext.BindInput(expressionTypeArgs[0]);

        return _queryContext.CreateQueryCall(method, [
            _queryContext.Tree,
            _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.Expression),
                inputParameter
            ])
        ]);
    }

    public override InterpolatedTree VisitSelectClause(SelectClauseSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
            return _queryContext.Tree;

        var argumentTypes = GetQueryMethodArgumentTypes(method);
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[0], out var expressionTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputParameter = _queryContext.BindInput(expressionTypeArgs[0]);

        return _queryContext.CreateQueryCall(method, [
            _queryContext.Tree,
            _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.Expression),
                inputParameter
            ])
        ]);
    }

    public override InterpolatedTree VisitWhereClause(WhereClauseSyntax node) {
        var qci = _context.SemanticModel.GetQueryClauseInfo(node);
        if(qci.OperationInfo.Symbol is not IMethodSymbol method)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var argumentTypes = GetQueryMethodArgumentTypes(method);
        if(!TryGetQueryDelegateTypeArgs(argumentTypes[0], out var expressionTypeArgs))
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(node);

        var inputParameter = _queryContext.BindInput(expressionTypeArgs[0]);

        return _queryContext.CreateQueryCall(method, [
            _queryContext.Tree,
            _builder.CreateExpression(nameof(Expression.Lambda), [
                Visit(node.Condition),
                inputParameter
            ])
        ]);
    }


    private InterpolatedTree CreateQueryInput(SyntaxNode clause, SyntaxNode inputNode, ISymbol? castSymbol) {
        var inputTree = Visit(inputNode);

        if(castSymbol is null)
            return inputTree;
        if(castSymbol is not IMethodSymbol castMethod)
            return _context.Diagnostics.UnsupportedInterpolatedSyntax(clause);

        return _queryContext.CreateQueryCall(castMethod, [inputTree]);
    }

    private sealed class QueryContext {
        public static QueryContext Create(InterpolatedSyntaxVisitor visitor) =>
            new QueryContext(default, visitor, "", InterpolatedTree.Unsupported, default!);

        private QueryContext(
            QueryContext? parentContext,
            InterpolatedSyntaxVisitor visitor,
            string inputIdentifier,
            InterpolatedTree tree,
            QueryBodySyntax queryBody
        ) {
            _parentContext = parentContext;
            _visitor = visitor;
            _builder = visitor._builder;
            _interpolatableIdentifiersSnapshot = visitor._interpolatableIdentifiers;
            InputIdentifier = inputIdentifier;
            Tree = tree;
            QueryBody = queryBody;
            _bindings = new(IdentifierEqualityComparer.Instance);
            _remappedTypes = new(SymbolEqualityComparer.IncludeNullability);
        }

        private readonly QueryContext? _parentContext;
        private readonly InterpolatedSyntaxVisitor _visitor;
        private readonly InterpolatedTreeBuilder _builder;
        private readonly ImmutableDictionary<string, InterpolatedTree> _interpolatableIdentifiersSnapshot;
        public string InputIdentifier { get; private set; }
        public string? JoinedIdentifier { get; private set; }
        public InterpolatedTree Tree { get; set; }
        public QueryBodySyntax QueryBody { get; }
        private Dictionary<string, InterpolatedTree> _bindings;
        private Dictionary<ITypeSymbol, ITypeSymbol> _remappedTypes;

        public QueryContext BindQuery(
            string inputIdentifier,
            InterpolatedTree tree,
            QueryBodySyntax queryBody
        ) =>
            new QueryContext(this, _visitor, inputIdentifier, tree, queryBody);

        public QueryContext Restore() {
            if(_parentContext is null)
                throw new InvalidOperationException();

            _visitor._interpolatableIdentifiers = _interpolatableIdentifiersSnapshot;
            return _parentContext;
        }

        /// <summary>
        /// Creates and binds an <see cref="InterpolatedTree"/> representing the query input parameter.
        /// </summary>
        /// <remarks>
        /// This call is necessary because the type of the query input is not actually known based
        /// on the initial <see cref="FromClauseSyntax"/>.
        /// </remarks>
        public InterpolatedTree BindInput(ITypeSymbol type) {
            var remappedType = GetRemappedType(type);
            var parameter = _builder.CreateParameter(remappedType, InputIdentifier);
            var inputPlaceholder = InterpolatedTree.Placeholder(InputIdentifier);
            _bindings[InputIdentifier] = inputPlaceholder;
            _visitor._interpolatableIdentifiers = _visitor._interpolatableIdentifiers.SetItem(InputIdentifier, parameter);
            return parameter;
        }

        /// <summary>
        /// Binds a value joined into the query tree with the specified <paramref name="identifier"/>
        /// to resolve to the provided <paramref name="tree"/>.
        /// </summary>
        public void BindJoined(string identifier, InterpolatedTree tree) {
            _bindings[identifier] = tree;
            _visitor._interpolatableIdentifiers = _visitor._interpolatableIdentifiers.SetItem(identifier, tree);
            JoinedIdentifier = identifier;
        }

        public ITypeSymbol RebindInput(ITypeSymbol type) {
            // Remap the input type, eliminating any transparent identifiers present in anonymous classes
            var reboundType = RemapType(type);

            // This is a pain in the butt; because the query syntax nodes are structured syntactically
            // instead of semantically, we have to process them bottom-up which involves maintaining
            // a second set of trees containing placeholder values which can then be replaced/remapped
            // to reference the updated query input each time we process a clause.
            var reboundIdentifier = _builder.CreateIdentifier();
            var reboundPlaceholder = InterpolatedTree.Placeholder(reboundIdentifier);
            var reboundParameter = _builder.CreateParameter(reboundType, reboundIdentifier);

            var existingPlaceholder = InterpolatedTree.Placeholder(InputIdentifier);

            // Create a tree to replace the existing input with an expression accessing the property
            // with the same name on the anonymous class representing the new input.
            var existingInputReplacement = _builder.CreateExpression(nameof(Expression.Property), [
                reboundPlaceholder,
                InterpolatedTree.Concat(
                    InterpolatedTree.InstanceCall(
                        _builder.CreateType(reboundType),
                        InterpolatedTree.Verbatim("GetProperty"),
                        [InterpolatedTree.Verbatim($"\"{InputIdentifier}\"")]
                    ),
                    InterpolatedTree.Verbatim("!")
                )
            ]);

            // Replace the existing input in our bindings with an accessor to the equivalent property
            // on the new input
            foreach(var binding in _bindings)
                _bindings[binding.Key] = binding.Value.Replace(existingPlaceholder, existingInputReplacement);

            // If we have a joined value, add a new binding for the equivalent property on the new input
            if(JoinedIdentifier is not null) {
                _bindings[JoinedIdentifier] = _builder.CreateExpression(nameof(Expression.Property), [
                    reboundPlaceholder,
                    InterpolatedTree.Concat(
                        InterpolatedTree.InstanceCall(
                            _builder.CreateType(reboundType),
                            InterpolatedTree.Verbatim("GetProperty"),
                            [InterpolatedTree.Verbatim($"\"{JoinedIdentifier}\"")]
                        ),
                        InterpolatedTree.Verbatim("!")
                    )
                ]);

                JoinedIdentifier = null;
            }

            // Dump all of the updated query bindings into our actual identifier bindings, replacing
            // the input placeholder with our new input parameter.
            foreach(var binding in _bindings)
                _visitor._interpolatableIdentifiers = _visitor._interpolatableIdentifiers.SetItem(
                    binding.Key,
                    binding.Value.Replace(reboundPlaceholder, reboundParameter)
                );

            // Finally, update the input identifier and return the remapped input type.
            InputIdentifier = reboundIdentifier;
            return reboundType;
        }

        public InterpolatedTree CreateQueryCall(IMethodSymbol method, IReadOnlyList<InterpolatedTree> arguments) {
            // Static extension method
            if(method is { ReducedFrom: { } })
                return _builder.CreateExpression(nameof(Expression.Call), [
                    _builder.CreateMethodInfo(GetRemappedMethod(method), default),
                    ..arguments
                ]);

            return _builder.CreateExpression(nameof(Expression.Call), [
                arguments[0],
                _builder.CreateMethodInfo(GetRemappedMethod(method), default),
                ..arguments.Skip(1)
            ]);
        }

        private ITypeSymbol RemapType(ITypeSymbol typeSymbol) {
            var typeKey = typeSymbol.WithNullableAnnotation(NullableAnnotation.None);
            if(_remappedTypes.TryGetValue(typeKey, out var remapped))
                return remapped.WithNullableAnnotation(typeSymbol.NullableAnnotation);

            if(typeSymbol is INamedTypeSymbol { IsAnonymousType: true } anonymous) {
                var properties = anonymous.GetMembers().OfType<IPropertySymbol>().ToList();
                if(properties.Count == 2 && properties[0].Name.StartsWith("<>", StringComparison.Ordinal)) {
                    remapped = _visitor._context.Compilation.CreateAnonymousTypeSymbol(
                        memberTypes: ImmutableArray.Create(GetRemappedType(properties[0].Type), GetRemappedType(properties[1].Type)),
                        memberNames: ImmutableArray.Create(InputIdentifier, properties[1].Name),
                        memberNullableAnnotations: ImmutableArray.Create(properties[0].NullableAnnotation, properties[1].NullableAnnotation)
                    );

                    _remappedTypes[typeKey] = remapped;
                    return remapped.WithNullableAnnotation(typeSymbol.NullableAnnotation);
                }
            }

            return typeSymbol;
        }

        private ITypeSymbol GetRemappedType(ITypeSymbol typeSymbol) =>
            TypeSymbolHelpers.ReplaceTypes(typeSymbol, _remappedTypes);

        private IMethodSymbol GetRemappedMethod(IMethodSymbol methodSymbol) {
            if(!methodSymbol.IsGenericMethod)
                return methodSymbol;

            var typeArguments = new ITypeSymbol[methodSymbol.TypeArguments.Length];
            for(var i = 0; i < typeArguments.Length; i++) {
                var typeArgument = methodSymbol.TypeArguments[i];
                typeArguments[i] = TypeSymbolHelpers.ReplaceTypes(typeArgument, _remappedTypes);
            }

            return methodSymbol.ConstructedFrom.Construct(typeArguments);
        }
    }
}
