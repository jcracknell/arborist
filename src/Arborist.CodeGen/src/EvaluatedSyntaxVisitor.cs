using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public class EvaluatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedTree> {
    private readonly InterpolatorInvocationContext _context;
    private readonly InterpolatedExpressionBuilder _builder;
    private readonly ImmutableHashSet<string> _interpolatableParameters;
    private ImmutableDictionary<string, InterpolatedTree> _evaluableParameters;

    public EvaluatedSyntaxVisitor(
        InterpolatorInvocationContext context,
        InterpolatedExpressionBuilder builder,
        ImmutableHashSet<string> interpolatableParameters
    ) {
        _context = context;
        _builder = builder;
        _interpolatableParameters = interpolatableParameters;
        _evaluableParameters = ImmutableDictionary.Create<string, InterpolatedTree>(IdentifierEqualityComparer.Instance);
    }

    public override InterpolatedTree Visit(SyntaxNode? node) {
        return base.Visit(node)!;
    }

    public override InterpolatedTree DefaultVisit(SyntaxNode node) {
        return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);
    }

    public override InterpolatedTree VisitConditionalExpression(ConditionalExpressionSyntax node) =>
        InterpolatedTree.Ternary(Visit(node.Condition), Visit(node.WhenTrue), Visit(node.WhenFalse));

    public override InterpolatedTree VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        // Replace references to the data property of the interpolation context with the
        // locally defined data referenceover.
        if(_context.IsInterpolationDataAccess(node))
            return InterpolatedTree.Verbatim(_builder.DataIdentifier);

        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not {} symbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);
        if(!TypeSymbolHelpers.IsAccessible(symbol))
            return _context.Diagnostics.InaccesibleSymbol(symbol, InterpolatedTree.Unsupported);

        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IFieldSymbol field when field.IsStatic || field.IsConst:
                if(!TypeSymbolHelpers.TryCreateTypeName(symbol.ContainingType, out var fieldContainingTypeName))
                    return _context.Diagnostics.UnsupportedType(field.ContainingType, InterpolatedTree.Unsupported);

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
                    return _context.Diagnostics.UnsupportedType(property.ContainingType, InterpolatedTree.Unsupported);

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
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);
        }
    }

    public override InterpolatedTree VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(node.Expression is not MemberAccessExpressionSyntax memberAccess)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Expression, InterpolatedTree.Unsupported);

        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IMethodSymbol method when method.IsStatic || method.IsExtensionMethod:
                if(!TypeSymbolHelpers.TryCreateTypeName(method.ContainingType, out var methodContainingTypeName))
                    return _context.Diagnostics.UnsupportedType(method.ContainingType, InterpolatedTree.Unsupported);

                return InterpolatedTree.StaticCall(
                    InterpolatedTree.Concat(
                        InterpolatedTree.Verbatim($"{methodContainingTypeName}."),
                        GetInvocationMethodName(memberAccess.Name)
                    ),
                    node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit).ToList()
                );

            case IMethodSymbol method:
                return InterpolatedTree.InstanceCall(
                    Visit(memberAccess.Expression),
                    GetInvocationMethodName(memberAccess.Name),
                    node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit).ToList()
                );

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);
        }
    }

    private InterpolatedTree GetInvocationMethodName(SimpleNameSyntax methodName) {
        // Emit explicitly passed type arguments
        if(methodName is not GenericNameSyntax generic)
            return InterpolatedTree.Verbatim(methodName.Identifier.Text);

        var typeArgumentNames = new List<string>(generic.TypeArgumentList.Arguments.Count);
        foreach(var typeArgument in generic.TypeArgumentList.Arguments) {
            if(_context.SemanticModel.GetTypeInfo(typeArgument).Type is not {} typeArgumentSymbol)
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(typeArgument, InterpolatedTree.Unsupported);
            if(!TypeSymbolHelpers.IsAccessible(typeArgumentSymbol))
                return _context.Diagnostics.InaccesibleSymbol(typeArgumentSymbol, InterpolatedTree.Unsupported);
            if(!TypeSymbolHelpers.TryCreateTypeName(typeArgumentSymbol, out var typeArgumentName))
                return _context.Diagnostics.UnsupportedType(typeArgumentSymbol, InterpolatedTree.Unsupported);

            typeArgumentNames.Add(typeArgumentName);
        }

        return InterpolatedTree.Verbatim(typeArgumentNames.MkString($"{generic.Identifier.Text}<", ", ", ">"));
    }

    public override InterpolatedTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) =>
        InterpolatedTree.Concat(
            InterpolatedTree.Verbatim("new "),
            InterpolatedTree.Initializer([..node.Initializers.Select(Visit)])
        );

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
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);

        if(!TypeSymbolHelpers.IsAccessible(nodeType))
            return _context.Diagnostics.InaccesibleSymbol(nodeType, InterpolatedTree.Unsupported);
        if(!TypeSymbolHelpers.TryCreateTypeName(nodeType, out var nodeTypeName))
            return _context.Diagnostics.UnsupportedType(nodeType, InterpolatedTree.Unsupported);

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
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);
        if(!TypeSymbolHelpers.IsAccessible(typeSymbol))
            return _context.Diagnostics.InaccesibleSymbol(typeSymbol, InterpolatedTree.Unsupported);
        if(!TypeSymbolHelpers.TryCreateTypeName(typeSymbol, out var typeName))
            return _context.Diagnostics.UnsupportedType(typeSymbol, InterpolatedTree.Unsupported);

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
                    return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);
                if(!TypeSymbolHelpers.IsAccessible(leftSymbol))
                    return _context.Diagnostics.InaccesibleSymbol(leftSymbol, InterpolatedTree.Unsupported);

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
        var evaluableSnapshot = _evaluableParameters;
        _evaluableParameters = _evaluableParameters.Add(
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
            _evaluableParameters = evaluableSnapshot;
        }
    }

    public override InterpolatedTree VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        // Add parameters defined by this lambda expression to the set of evaluable parameters
        var evaluableSnapshot = _evaluableParameters;
        _evaluableParameters = _evaluableParameters.SetItems(
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
            _evaluableParameters = evaluableSnapshot;
        }
    }

    public override InterpolatedTree VisitIdentifierName(IdentifierNameSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not {} symbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);

        if(_evaluableParameters.TryGetValue(node.Identifier.Text, out var mappedTree))
            return mappedTree;

        if(_interpolatableParameters.Contains(node.Identifier.Text))
            return _context.Diagnostics.EvaluatedParameter(node, InterpolatedTree.Unsupported);

        return _context.Diagnostics.Closure(node, InterpolatedTree.Unsupported);
    }

    public override InterpolatedTree VisitParameter(ParameterSyntax node) {
        if(node.Type is null)
            return InterpolatedTree.Verbatim(node.Identifier.Text);

        if(_context.SemanticModel.GetTypeInfo(node.Type).Type is not {} parameterType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedTree.Unsupported);
        if(!TypeSymbolHelpers.IsAccessible(parameterType))
            return _context.Diagnostics.InaccesibleSymbol(parameterType, InterpolatedTree.Unsupported);
        if(!TypeSymbolHelpers.TryCreateTypeName(parameterType, out var parameterTypeName))
            return _context.Diagnostics.UnsupportedType(parameterType, InterpolatedTree.Unsupported);

        return InterpolatedTree.Verbatim($"{parameterTypeName} {node.Identifier.Text}");
    }

    public override InterpolatedTree VisitBinaryExpression(BinaryExpressionSyntax node) =>
        InterpolatedTree.Binary(node.OperatorToken.ToString(), Visit(node.Left), Visit(node.Right));

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

    public override InterpolatedTree VisitLiteralExpression(LiteralExpressionSyntax node) =>
        InterpolatedTree.Verbatim(node.ToFullString());
}
