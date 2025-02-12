using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedTree> {
    private readonly InterpolationAnalysisContext _context;
    private readonly InterpolatedTreeBuilder _builder;
    private readonly ImmutableHashSet<string> _interpolatableParameters;
    private ImmutableDictionary<string, InterpolatedTree> _evaluableIdentifiers;
    private QueryContext _queryContext;

    public EvaluatedSyntaxVisitor(
        InterpolationAnalysisContext context,
        ImmutableHashSet<string> interpolatableParameters
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
        // locally defined data reference.
        if(_context.IsInterpolationDataAccess(node))
            return InterpolatedTree.Verbatim(_builder.DataIdentifier);

        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not {} symbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!TypeSymbolHelpers.IsAccessible(symbol))
            return _context.Diagnostics.InaccessibleSymbol(symbol, node);

        switch(symbol) {
            case IFieldSymbol field when field.IsStatic || field.IsConst:
                var fieldContainingTypeName = _builder.CreateTypeName(symbol.ContainingType, node);

                return InterpolatedTree.Member(
                    fieldContainingTypeName,
                    InterpolatedTree.Verbatim(node.Name.ToString())
                );

            case IFieldSymbol:
                return InterpolatedTree.Member(
                    Visit(node.Expression),
                    InterpolatedTree.Verbatim(node.Name.ToString())
                );

            case IPropertySymbol { IsStatic: true } property:
                var propertyContainingTypeName = _builder.CreateTypeName(property.ContainingType, node);

                return InterpolatedTree.Member(
                    propertyContainingTypeName,
                    InterpolatedTree.Verbatim(node.Name.ToString())
                );

            case IPropertySymbol:
                return InterpolatedTree.Member(
                    Visit(node.Expression),
                    InterpolatedTree.Verbatim(node.Name.ToString())
                );

            case IMethodSymbol:
                var methodName = GetInvocationMethodName(node.Parent, node.Name);
                return InterpolatedTree.Interpolate($"{Visit(node.Expression)}.{methodName}");

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        }
    }

    public override InterpolatedTree VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        switch(methodSymbol) {
            // If this is an extension method invoked as a postfix method, then we need to rewrite the call
            // as a static method invocation so we don't have to import the extension into scope.
            case { ReducedFrom: { IsStatic: true } }:
                if(!TypeSymbolHelpers.IsAccessible(methodSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(methodSymbol, node);

                // A postfix extension method invocation should always be MemberAccessExpressionSyntax
                // of the form subject.Extension(...)
                if(node.Expression is not MemberAccessExpressionSyntax memberAccess)
                    return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

                var extensionTypeName = _builder.CreateTypeName(methodSymbol.ContainingType, node);
                var methodName = GetInvocationMethodName(node, memberAccess.Name);

                return InterpolatedTree.Call(
                    InterpolatedTree.Interpolate($"{extensionTypeName}.{methodName}"),
                    [
                        Visit(memberAccess.Expression),
                        ..node.ArgumentList.Arguments.SelectEager(a => Visit(a.Expression))
                    ]
                );

            default:
                return InterpolatedTree.Call(
                    Visit(node.Expression),
                    node.ArgumentList.Arguments.SelectEager(a => Visit(a.Expression))
                );
        }
    }

    private InterpolatedTree GetInvocationMethodName(SyntaxNode? invocationNode, SimpleNameSyntax methodName) {
        // Only emit explicitly passed type arguments
        if(!SyntaxHelpers.IsExplicitGenericMethodInvocation(invocationNode) || methodName is not GenericNameSyntax generic)
            return InterpolatedTree.Verbatim(methodName.Identifier.Text);

        var typeArgumentParts = new List<InterpolatedTree>(2 * generic.TypeArgumentList.Arguments.Count - 1);
        for(var i = 0; i < generic.TypeArgumentList.Arguments.Count; i++) {
            var typeArgument = generic.TypeArgumentList.Arguments[i];
            if(_context.SemanticModel.GetTypeInfo(typeArgument).Type is not {} typeArgumentSymbol)
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(typeArgument);

            if(i != 0)
                typeArgumentParts.Add(InterpolatedTree.Verbatim(", "));

            typeArgumentParts.Add(_builder.CreateTypeName(typeArgumentSymbol, typeArgument));
        }

        return InterpolatedTree.Interpolate($"{generic.Identifier.Text}<{InterpolatedTree.Concat(typeArgumentParts)}>");
    }

    public override InterpolatedTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) =>
        InterpolatedTree.AnonymousClass([..node.Initializers.Select(Visit)]);

    public override InterpolatedTree? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node) =>
        node.NameEquals switch {
            null => Visit(node.Expression),
            not null => InterpolatedTree.Interpolate($"{node.NameEquals.Name.Identifier.Text} = {Visit(node.Expression)}")
        };

    public override InterpolatedTree VisitCheckedExpression(CheckedExpressionSyntax node) {
        return InterpolatedTree.Call(InterpolatedTree.Verbatim(node.Keyword.ValueText), [
            Visit(node.Expression)
        ]);
    }

    public override InterpolatedTree VisitDefaultExpression(DefaultExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        return _builder.CreateDefaultValue(typeSymbol.WithNullableAnnotation(NullableAnnotation.Annotated));
    }

    public override InterpolatedTree VisitCastExpression(CastExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} nodeType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        var nodeTypeName = _builder.CreateTypeName(nodeType, node);
        return InterpolatedTree.Interpolate($"({nodeTypeName}){Visit(node.Expression)}");
    }

    public override InterpolatedTree VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    public override InterpolatedTree VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    private InterpolatedTree VisitBaseObjectCreationExpression(BaseObjectCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        var newExpr = InterpolatedTree.Call(
            node switch {
                ImplicitObjectCreationExpressionSyntax => InterpolatedTree.Verbatim("new"),
                _ => InterpolatedTree.Interpolate($"new {_builder.CreateTypeName(typeSymbol, node)}")
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

    public override InterpolatedTree VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) =>
        InterpolatedTree.Interpolate($"new[] {Visit(node.Initializer)}");

    public override InterpolatedTree VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not IArrayTypeSymbol typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        var elementType = TypeSymbolHelpers.GetRootArrayElementType(typeSymbol);
        if(!_builder.TryCreateTypeName(elementType, out var elementTypeName))
            return _context.Diagnostics.UnsupportedType(typeSymbol, node);

        // Multi-dimensional array initializers are forbidden in expression trees, so if there is
        // an initializer then it's an implicit single-dimensional array
        if(node.Initializer is not null)
            return InterpolatedTree.Interpolate($"new {elementTypeName}[] {Visit(node.Initializer)}");

        var rankSpecifiers = InterpolatedTree.Concat(node.Type.RankSpecifiers.SelectEager(Visit));
        return InterpolatedTree.Interpolate($"new {elementTypeName}{rankSpecifiers}");
    }

    public override InterpolatedTree VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node) {
        var parts = new List<InterpolatedTree>(2 * node.Sizes.Count + 1);
        parts.Add(InterpolatedTree.Verbatim("["));

        for(var i = 0; i < node.Sizes.Count; i++) {
            if(i != 0)
                parts.Add(InterpolatedTree.Verbatim(", "));

            parts.Add(Visit(node.Sizes[i]));
        }

        parts.Add(InterpolatedTree.Verbatim("]"));
        return InterpolatedTree.Concat(parts);
    }

    public override InterpolatedTree VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node) =>
        InterpolatedTree.Empty;

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

                return InterpolatedTree.Interpolate($"{assignment.Left} = {Visit(assignment.Right)}");

            default:
                return Visit(node);
        }
    }

    public override InterpolatedTree VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
        InterpolatedTree.Interpolate($"{Visit(node.Left)} = {Visit(node.Right)}");

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

        switch(symbol) {
            case ITypeSymbol typeSymbol:
                return _builder.CreateTypeName(typeSymbol, node);

            case IFieldSymbol fieldSymbol:
                // N.B. for consistency accessibility takes precedence over the closure warning because you can't
                // know about the closure when processing a member access
                if(!TypeSymbolHelpers.IsAccessible(fieldSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(symbol, node);
                if(!fieldSymbol.IsStatic)
                    return _context.Diagnostics.ClosureOverScopeReference(node);

                return InterpolatedTree.Concat(
                    _builder.CreateTypeName(fieldSymbol.ContainingType, node),
                    InterpolatedTree.Verbatim("."),
                    InterpolatedTree.Verbatim(fieldSymbol.Name)
                );

            case IPropertySymbol propertySymbol:
                if(!TypeSymbolHelpers.IsAccessible(propertySymbol))
                    return _context.Diagnostics.InaccessibleSymbol(symbol, node);
                if(!propertySymbol.IsStatic)
                    return _context.Diagnostics.ClosureOverScopeReference(node);

                return InterpolatedTree.Concat(
                    _builder.CreateTypeName(propertySymbol.ContainingType, node),
                    InterpolatedTree.Verbatim("."),
                    InterpolatedTree.Verbatim(propertySymbol.Name)
                );

            case IMethodSymbol methodSymbol:
                if(!TypeSymbolHelpers.IsAccessible(methodSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(symbol, node);
                if(!methodSymbol.IsStatic)
                    return _context.Diagnostics.ClosureOverScopeReference(node);

                var typeName = _builder.CreateTypeName(methodSymbol.ContainingType, node);
                var methodName = GetInvocationMethodName(node.Parent, node);
                return InterpolatedTree.Interpolate($"{typeName}.{methodName}");

            default:
                if(_evaluableIdentifiers.TryGetValue(node.Identifier.ValueText, out var mappedTree))
                    return mappedTree;

                if(_interpolatableParameters.Contains(node.Identifier.ValueText))
                    return _context.Diagnostics.EvaluatedParameter(node);

                return _context.Diagnostics.ClosureOverScopeReference(node);
        }
    }

    public override InterpolatedTree VisitParameter(ParameterSyntax node) {
        if(node.Type is null)
            return InterpolatedTree.Verbatim(node.Identifier.Text);

        if(_context.SemanticModel.GetTypeInfo(node.Type).Type is not {} parameterType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        var parameterTypeName = _builder.CreateTypeName(parameterType, node);

        return InterpolatedTree.Interpolate($"{parameterTypeName} {node.Identifier.Text}");
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

        return InterpolatedTree.Binary(
            node.OperatorToken.ToString(),
            Visit(node.Left),
            _builder.CreateTypeName(typeOperand, node.Right)
        );
    }

    private InterpolatedTree VisitBinaryIsExpression(BinaryExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node.Right).Type is not {} typeOperand)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Right);

        return InterpolatedTree.Binary(
            node.OperatorToken.ToString(),
            Visit(node.Left),
            _builder.CreateTypeName(typeOperand, node.Right)
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
        node.Kind() switch {
            SyntaxKind.DefaultLiteralExpression => VisitDefaultLiteralExpression(node),
            _ => InterpolatedTree.Verbatim(node.ToString().Trim())
        };

    private InterpolatedTree VisitDefaultLiteralExpression(LiteralExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        return _builder.CreateDefaultValue(typeSymbol.WithNullableAnnotation(NullableAnnotation.Annotated));
    }
}
