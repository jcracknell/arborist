using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public partial class EvaluatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedTree> {
    private readonly InterpolationAnalysisContext _context;
    private readonly InterpolatedTreeBuilder _builder;
    private readonly ImmutableDictionary<string, InterpolatedTree> _interpolatableParameters;
    private ImmutableDictionary<string, InterpolatedTree> _evaluableIdentifiers;
    private QueryContext _queryContext;

    public EvaluatedSyntaxVisitor(
        InterpolationAnalysisContext context,
        ImmutableDictionary<string, InterpolatedTree> interpolatableParameters
    ) {
        _context = context;
        _builder = context.TreeBuilder;
        _interpolatableParameters = interpolatableParameters;
        _evaluableIdentifiers = ImmutableDictionary.Create<string, InterpolatedTree>(IdentifierEqualityComparer.Instance);
        _queryContext = QueryContext.Create(this);
    }

    public override InterpolatedTree Visit(SyntaxNode? node) {
        // Check for cancellation every time we visit (iterate) over a node
        _context.CancellationToken.ThrowIfCancellationRequested();

        return base.Visit(node)!;
    }

    public override InterpolatedTree DefaultVisit(SyntaxNode node) {
        return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
    }

    public override InterpolatedTree VisitConditionalExpression(ConditionalExpressionSyntax node) =>
        InterpolatedTree.Ternary(Visit(node.Condition), Visit(node.WhenTrue), Visit(node.WhenFalse));

    public override InterpolatedTree VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        // Replace references to the data property of the interpolation context with the
        // locally defined data referenceover.
        if(_context.IsInterpolationDataAccess(node))
            return InterpolatedTree.Verbatim(_builder.DataIdentifier);

        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not {} symbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(symbol))
            return _context.Diagnostics.InaccessibleSymbol(symbol, node);

        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IFieldSymbol field when field.IsStatic || field.IsConst:
                if(!TypeSymbolHelpers.TryCreateTypeName(symbol.ContainingType, out var fieldContainingTypeName))
                    return _context.Diagnostics.UnsupportedType(field.ContainingType, node);

                return InterpolatedTree.Member(
                    InterpolatedTree.Verbatim(fieldContainingTypeName),
                    InterpolatedTree.Verbatim(node.Name.ToString())
                );

            case IFieldSymbol field:
                return InterpolatedTree.Member(
                    Visit(node.Expression),
                    InterpolatedTree.Verbatim(node.Name.ToString())
                );

            case IPropertySymbol { IsStatic: true } property:
                if(!TypeSymbolHelpers.TryCreateTypeName(property.ContainingType, out var propertyContainingTypeName))
                    return _context.Diagnostics.UnsupportedType(property.ContainingType, node);

                return InterpolatedTree.Member(
                    InterpolatedTree.Verbatim(propertyContainingTypeName),
                    InterpolatedTree.Verbatim(node.Name.ToString())
                );

            case IPropertySymbol property:
                return InterpolatedTree.Member(
                    Visit(node.Expression),
                    InterpolatedTree.Verbatim(node.Name.ToString())
                );

            case IMethodSymbol method:
                return Visit(node.Expression);

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        }
    }

    public override InterpolatedTree VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(node.Expression is not MemberAccessExpressionSyntax memberAccess)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Expression);

        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IMethodSymbol { ReducedFrom: { IsStatic: true } } method:
                if(!TypeSymbolHelpers.TryCreateTypeName(method.ContainingType, out var extensionTypeName))
                    return _context.Diagnostics.UnsupportedType(method.ContainingType, node);

                return InterpolatedTree.StaticCall(
                    InterpolatedTree.Concat(
                        InterpolatedTree.Verbatim($"{extensionTypeName}."),
                        GetInvocationMethodName(node, memberAccess.Name)
                    ),
                    [
                        Visit(node.Expression),
                        ..node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit)
                    ]
                );

            case IMethodSymbol { IsStatic: true } method:
                if(!TypeSymbolHelpers.TryCreateTypeName(method.ContainingType, out var staticTypeName))
                    return _context.Diagnostics.UnsupportedType(method.ContainingType, node);

                return InterpolatedTree.StaticCall(
                    InterpolatedTree.Concat(
                        InterpolatedTree.Verbatim($"{staticTypeName}."),
                        GetInvocationMethodName(node, memberAccess.Name)
                    ),
                    [..node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit)]
                );

            case IMethodSymbol method:
                return InterpolatedTree.InstanceCall(
                    Visit(memberAccess.Expression),
                    GetInvocationMethodName(node, memberAccess.Name),
                    node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit).ToList()
                );

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        }
    }

    private InterpolatedTree GetInvocationMethodName(InvocationExpressionSyntax node, SimpleNameSyntax methodName) {
        // Emit explicitly passed type arguments
        if(methodName is not GenericNameSyntax generic)
            return InterpolatedTree.Verbatim(methodName.Identifier.Text);

        var typeArgumentNames = new List<string>(generic.TypeArgumentList.Arguments.Count);
        foreach(var typeArgument in generic.TypeArgumentList.Arguments) {
            if(_context.SemanticModel.GetTypeInfo(typeArgument).Type is not {} typeArgumentSymbol)
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(typeArgument);
            if(!TypeSymbolHelpers.IsAccessible(typeArgumentSymbol))
                return _context.Diagnostics.InaccessibleSymbol(typeArgumentSymbol, node);
            if(!TypeSymbolHelpers.TryCreateTypeName(typeArgumentSymbol, out var typeArgumentName))
                return _context.Diagnostics.UnsupportedType(typeArgumentSymbol, node);

            typeArgumentNames.Add(typeArgumentName);
        }

        return InterpolatedTree.Verbatim(typeArgumentNames.MkString($"{generic.Identifier.Text}<", ", ", ">"));
    }

    public override InterpolatedTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) =>
        InterpolatedTree.AnonymousClass([..node.Initializers.Select(Visit)]);

    public override InterpolatedTree? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node) =>
        node.NameEquals switch {
            null => Visit(node.Expression),
            not null => InterpolatedTree.Concat(
                InterpolatedTree.Verbatim($"{node.NameEquals.Name.Identifier.Text} = "),
                Visit(node.Expression)
            )
        };

    public override InterpolatedTree VisitCastExpression(CastExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} nodeType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        if(!TypeSymbolHelpers.IsAccessible(nodeType))
            return _context.Diagnostics.InaccessibleSymbol(nodeType, node);
        if(!TypeSymbolHelpers.TryCreateTypeName(nodeType, out var nodeTypeName))
            return _context.Diagnostics.UnsupportedType(nodeType, node);

        return InterpolatedTree.Concat(
            InterpolatedTree.Verbatim($"({nodeTypeName})"),
            Visit(node.Expression)
        );
    }

    public override InterpolatedTree VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    public override InterpolatedTree VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    private InterpolatedTree VisitBaseObjectCreationExpression(BaseObjectCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(typeSymbol))
            return _context.Diagnostics.InaccessibleSymbol(typeSymbol, node);
        if(!TypeSymbolHelpers.TryCreateTypeName(typeSymbol, out var typeName))
            return _context.Diagnostics.UnsupportedType(typeSymbol, node);

        var newExpr = InterpolatedTree.StaticCall(
            node switch {
                ImplicitObjectCreationExpressionSyntax => InterpolatedTree.Verbatim("new"),
                _ => InterpolatedTree.Verbatim($"new {typeName}")
            },
            [..(node.ArgumentList switch {
                null => Array.Empty<InterpolatedTree>(),
                not null => node.ArgumentList.Arguments.Select(a => Visit(a.Expression))
            })]
        );

        if(node.Initializer is null)
            return newExpr;

        return InterpolatedTree.Concat(newExpr, Visit(node.Initializer));
    }

    public override InterpolatedTree VisitInitializerExpression(InitializerExpressionSyntax node) =>
        InterpolatedTree.Initializer([..node.Expressions.Select(VisitInitializerElement)]);

    private InterpolatedTree VisitInitializerElement(ExpressionSyntax node) {
        switch(node) {
            // This requires handling as a special case of IdentifierNameSyntax
            case AssignmentExpressionSyntax assignment:
                if(_context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol is not {} leftSymbol)
                    return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
                if(!TypeSymbolHelpers.IsAccessible(leftSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(leftSymbol, assignment.Left);

                return InterpolatedTree.Concat(
                    InterpolatedTree.Verbatim(assignment.Left.ToString()),
                    InterpolatedTree.Verbatim(" = "),
                    Visit(assignment.Right)
                );

            default:
                return Visit(node);
        }
    }

    public override InterpolatedTree VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
        InterpolatedTree.Concat(
            Visit(node.Left),
            InterpolatedTree.Verbatim(" = "),
            Visit(node.Right)
        );

    public override InterpolatedTree VisitElementBindingExpression(ElementBindingExpressionSyntax node) {
        var arguments = node.ArgumentList.Arguments;
        var trees = new List<InterpolatedTree>(2 * arguments.Count + 1);
        trees.Add(InterpolatedTree.Verbatim("{ "));
        trees.Add(Visit(arguments[0]));
        for(var i = 1; i < arguments.Count; i++) {
            trees.Add(InterpolatedTree.Verbatim(", "));
            trees.Add(Visit(arguments[i]));
        }
        trees.Add(InterpolatedTree.Verbatim(" }"));

        return InterpolatedTree.Concat(trees);
    }

    public override InterpolatedTree VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
        // Add parameters defined by this lambda expression to the set of evaluable parameters
        var evaluableSnapshot = _evaluableIdentifiers;
        _evaluableIdentifiers = _evaluableIdentifiers.Add(
            node.Parameter.Identifier.Text,
            InterpolatedTree.Verbatim(node.Parameter.Identifier.Text)
        );
        try {
            var lambda = InterpolatedTree.Lambda([Visit(node.Parameter)], Visit(node.Body));

            if(node.Modifiers.Count == 0)
                return lambda;

            return InterpolatedTree.Concat(
                InterpolatedTree.Verbatim(node.Modifiers.MkString("", m => m.Text, " ", " ")),
                lambda
            );
        } finally {
            _evaluableIdentifiers = evaluableSnapshot;
        }
    }

    public override InterpolatedTree VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        // Add parameters defined by this lambda expression to the set of evaluable parameters
        var evaluableSnapshot = _evaluableIdentifiers;
        _evaluableIdentifiers = _evaluableIdentifiers.SetItems(
            from p in node.ParameterList.Parameters
            select new KeyValuePair<string, InterpolatedTree>(p.Identifier.Text, InterpolatedTree.Verbatim(p.Identifier.Text))
        );
        try {
            var lambda = InterpolatedTree.Lambda(
                node.ParameterList.Parameters.Select(Visit).ToList(),
                Visit(node.Body)
            );

            if(node.Modifiers.Count == 0)
                return lambda;

            return InterpolatedTree.Concat(
                InterpolatedTree.Verbatim(node.Modifiers.MkString("", m => m.Text, " ", " ")),
                lambda
            );
        } finally {
            _evaluableIdentifiers = evaluableSnapshot;
        }
    }

    public override InterpolatedTree VisitIdentifierName(IdentifierNameSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not {} symbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        if(_evaluableIdentifiers.TryGetValue(node.Identifier.Text, out var mappedTree))
            return mappedTree;

        if(_interpolatableParameters.ContainsKey(node.Identifier.Text))
            return _context.Diagnostics.EvaluatedParameter(node);

        return _context.Diagnostics.ClosureOverScopeReference(node);
    }

    public override InterpolatedTree VisitParameter(ParameterSyntax node) {
        if(node.Type is null)
            return InterpolatedTree.Verbatim(node.Identifier.Text);

        if(_context.SemanticModel.GetTypeInfo(node.Type).Type is not {} parameterType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(parameterType))
            return _context.Diagnostics.InaccessibleSymbol(parameterType, node.Type);
        if(!TypeSymbolHelpers.TryCreateTypeName(parameterType, out var parameterTypeName))
            return _context.Diagnostics.UnsupportedType(parameterType, node.Type);

        return InterpolatedTree.Verbatim($"{parameterTypeName} {node.Identifier.Text}");
    }

    public override InterpolatedTree VisitBinaryExpression(BinaryExpressionSyntax node) {
        if(TryVisitBinarySpecialExpression(node, out var special))
            return special;

        return InterpolatedTree.Binary(node.OperatorToken.ToString(), Visit(node.Left), Visit(node.Right));
    }

    private bool TryVisitBinarySpecialExpression(
        BinaryExpressionSyntax node,
        [NotNullWhen(true)] out InterpolatedTree? result
    ) {
        switch(node.Kind()) {
            case SyntaxKind.AsExpression:
                result = VisitBinaryAsExpression(node);
                return true;
            case SyntaxKind.IsExpression:
                result = VisitBinaryIsExpression(node);
                return true;
            default:
                result = default;
                return false;
        }
    }

    private InterpolatedTree VisitBinaryAsExpression(BinaryExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node.Right).Type is not {} typeOperand)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Right);
        if(!TypeSymbolHelpers.IsAccessible(typeOperand))
            return _context.Diagnostics.InaccessibleSymbol(typeOperand, node.Right);
        if(!TypeSymbolHelpers.TryCreateTypeName(typeOperand, out var typeName))
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Right);

        return InterpolatedTree.Binary(
            node.OperatorToken.ToString(),
            Visit(node.Left),
            InterpolatedTree.Verbatim(typeName)
        );
    }

    private InterpolatedTree VisitBinaryIsExpression(BinaryExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node.Right).Type is not {} typeOperand)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Right);
        if(!TypeSymbolHelpers.IsAccessible(typeOperand))
            return _context.Diagnostics.InaccessibleSymbol(typeOperand, node.Right);
        if(!TypeSymbolHelpers.TryCreateTypeName(typeOperand, out var typeName))
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Right);

        return InterpolatedTree.Binary(
            node.OperatorToken.ToString(),
            Visit(node.Left),
            InterpolatedTree.Verbatim(typeName)
        );
    }

    public override InterpolatedTree? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) =>
        InterpolatedTree.Concat(
            InterpolatedTree.Verbatim(node.OperatorToken.ToString()),
            Visit(node.Operand)
        );

    public override InterpolatedTree VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) =>
        InterpolatedTree.Concat(
            Visit(node.Operand),
            InterpolatedTree.Verbatim(node.OperatorToken.ToString())
        );

    public override InterpolatedTree VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) =>
        Visit(node.Expression);

    public override InterpolatedTree VisitLiteralExpression(LiteralExpressionSyntax node) =>
        InterpolatedTree.Verbatim(node.ToString().Trim());

}
