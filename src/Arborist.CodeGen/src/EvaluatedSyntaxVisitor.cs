using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.CodeGen;

public class EvaluatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedExpressionTree> {
    private readonly InterpolatorInvocationContext _context;
    private readonly InterpolatedExpressionBuilder _builder;
    private readonly ImmutableHashSet<string> _interpolatableParameters;
    private ImmutableHashSet<string> _evaluableParameters;

    public EvaluatedSyntaxVisitor(
        InterpolatorInvocationContext context,
        InterpolatedExpressionBuilder builder,
        ImmutableHashSet<string> interpolatableParameters
    ) {
        _context = context;
        _builder = builder;
        _interpolatableParameters = interpolatableParameters;
        _evaluableParameters = ImmutableHashSet<string>.Empty.WithComparer(IdentifierEqualityComparer.Instance);
    }

    public override InterpolatedExpressionTree Visit(SyntaxNode? node) {
        return base.Visit(node)!;
    }

    public override InterpolatedExpressionTree DefaultVisit(SyntaxNode node) {
        return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);
    }

    public override InterpolatedExpressionTree VisitConditionalExpression(ConditionalExpressionSyntax node) =>
        InterpolatedExpressionTree.Ternary(Visit(node.Condition), Visit(node.WhenTrue), Visit(node.WhenFalse));

    public override InterpolatedExpressionTree VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        // Replace references to the data property of the interpolation context with the
        // locally defined data referenceover.
        if(_context.IsInterpolationDataAccess(node))
            return InterpolatedExpressionTree.Verbatim(_builder.DataIdentifier);

        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not {} symbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        if(!TypeSymbolHelpers.IsAccessible(symbol))
            return _context.Diagnostics.InaccesibleSymbol(symbol, InterpolatedExpressionTree.Unsupported);

        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IFieldSymbol field when field.IsStatic || field.IsConst:
                if(!TypeSymbolHelpers.TryCreateTypeName(symbol.ContainingType, out var fieldContainingTypeName))
                    return _context.Diagnostics.UnsupportedType(field.ContainingType, InterpolatedExpressionTree.Unsupported);

                return InterpolatedExpressionTree.Member(
                    InterpolatedExpressionTree.Verbatim(fieldContainingTypeName),
                    node.Name.ToString()
                );

            case IFieldSymbol field:
                return InterpolatedExpressionTree.Member(
                    Visit(node.Expression),
                    node.Name.ToString()
                );

            case IPropertySymbol { IsStatic: true } property:
                if(!TypeSymbolHelpers.TryCreateTypeName(property.ContainingType, out var propertyContainingTypeName))
                    return _context.Diagnostics.UnsupportedType(property.ContainingType, InterpolatedExpressionTree.Unsupported);

                return InterpolatedExpressionTree.Member(
                    InterpolatedExpressionTree.Verbatim(propertyContainingTypeName),
                    node.Name.ToString()
                );

            case IPropertySymbol property:
                return InterpolatedExpressionTree.Member(
                    Visit(node.Expression),
                    node.Name.ToString()
                );

            case IMethodSymbol method:
                return Visit(node.Expression);

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        }
    }

    public override InterpolatedExpressionTree VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(node.Expression is not MemberAccessExpressionSyntax memberAccess)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Expression, InterpolatedExpressionTree.Unsupported);

        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IMethodSymbol method when method.IsStatic || method.IsExtensionMethod:
                if(!TypeSymbolHelpers.TryCreateTypeName(method.ContainingType, out var methodContainingTypeName))
                    return _context.Diagnostics.UnsupportedType(method.ContainingType, InterpolatedExpressionTree.Unsupported);

                return InterpolatedExpressionTree.StaticCall(
                    InterpolatedExpressionTree.Concat(
                        $"{methodContainingTypeName}.",
                        GetInvocationMethodName(memberAccess.Name)
                    ),
                    node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit).ToList()
                );

            case IMethodSymbol method:
                return InterpolatedExpressionTree.InstanceCall(
                    Visit(memberAccess.Expression),
                    GetInvocationMethodName(memberAccess.Name),
                    node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit).ToList()
                );

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        }
    }

    private InterpolatedExpressionTree GetInvocationMethodName(SimpleNameSyntax methodName) {
        // Emit explicitly passed type arguments
        if(methodName is not GenericNameSyntax generic)
            return InterpolatedExpressionTree.Verbatim(methodName.Identifier.Text);

        var typeArgumentNames = new List<string>(generic.TypeArgumentList.Arguments.Count);
        foreach(var typeArgument in generic.TypeArgumentList.Arguments) {
            if(_context.SemanticModel.GetTypeInfo(typeArgument).Type is not {} typeArgumentSymbol)
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(typeArgument, InterpolatedExpressionTree.Unsupported);
            if(!TypeSymbolHelpers.IsAccessible(typeArgumentSymbol))
                return _context.Diagnostics.InaccesibleSymbol(typeArgumentSymbol, InterpolatedExpressionTree.Unsupported);
            if(!TypeSymbolHelpers.TryCreateTypeName(typeArgumentSymbol, out var typeArgumentName))
                return _context.Diagnostics.UnsupportedType(typeArgumentSymbol, InterpolatedExpressionTree.Unsupported);

            typeArgumentNames.Add(typeArgumentName);
        }

        return InterpolatedExpressionTree.Verbatim(typeArgumentNames.MkString($"{generic.Identifier.Text}<", ", ", ">"));
    }

    public override InterpolatedExpressionTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) =>
        InterpolatedExpressionTree.Concat(
            InterpolatedExpressionTree.Verbatim("new "),
            InterpolatedExpressionTree.Initializer([..node.Initializers.Select(Visit)])
        );

    public override InterpolatedExpressionTree? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node) =>
        node.NameEquals switch {
            null => Visit(node.Expression),
            not null => InterpolatedExpressionTree.Concat(
                InterpolatedExpressionTree.Verbatim($"{node.NameEquals.Name.Identifier.Text} = "),
                Visit(node.Expression)
            )
        };

    public override InterpolatedExpressionTree VisitCastExpression(CastExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} nodeType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);

        if(!TypeSymbolHelpers.IsAccessible(nodeType))
            return _context.Diagnostics.InaccesibleSymbol(nodeType, InterpolatedExpressionTree.Unsupported);
        if(!TypeSymbolHelpers.TryCreateTypeName(nodeType, out var nodeTypeName))
            return _context.Diagnostics.UnsupportedType(nodeType, InterpolatedExpressionTree.Unsupported);

        return InterpolatedExpressionTree.Concat(
            InterpolatedExpressionTree.Verbatim($"({nodeTypeName})"),
            Visit(node.Expression)
        );
    }

    public override InterpolatedExpressionTree VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    public override InterpolatedExpressionTree VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    private InterpolatedExpressionTree VisitBaseObjectCreationExpression(BaseObjectCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        if(!TypeSymbolHelpers.IsAccessible(typeSymbol))
            return _context.Diagnostics.InaccesibleSymbol(typeSymbol, InterpolatedExpressionTree.Unsupported);
        if(!TypeSymbolHelpers.TryCreateTypeName(typeSymbol, out var typeName))
            return _context.Diagnostics.UnsupportedType(typeSymbol, InterpolatedExpressionTree.Unsupported);

        var newExpr = InterpolatedExpressionTree.StaticCall(
            node switch {
                ImplicitObjectCreationExpressionSyntax => "new",
                _ => $"new {typeName}"
            },
            [..(node.ArgumentList switch {
                null => Array.Empty<InterpolatedExpressionTree>(),
                not null => node.ArgumentList.Arguments.Select(a => Visit(a.Expression))
            })]
        );

        if(node.Initializer is null)
            return newExpr;

        return InterpolatedExpressionTree.Concat(
            newExpr,
            InterpolatedExpressionTree.Initializer([..node.Initializer.Expressions.Select(Visit)])
        );
    }

    public override InterpolatedExpressionTree? VisitInitializerExpression(InitializerExpressionSyntax node) =>
        InterpolatedExpressionTree.Initializer([..node.Expressions.Select(Visit)]);

    public override InterpolatedExpressionTree VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
        InterpolatedExpressionTree.Concat(
            Visit(node.Left),
            InterpolatedExpressionTree.Verbatim(" = "),
            Visit(node.Right)
        );

    public override InterpolatedExpressionTree VisitElementBindingExpression(ElementBindingExpressionSyntax node) {
        var arguments = node.ArgumentList.Arguments;
        var trees = new List<InterpolatedExpressionTree>(2 * arguments.Count + 1);
        trees.Add(InterpolatedExpressionTree.Verbatim("{ "));
        trees.Add(Visit(arguments[0]));
        for(var i = 1; i < arguments.Count; i++) {
            trees.Add(InterpolatedExpressionTree.Verbatim(", "));
            trees.Add(Visit(arguments[i]));
        }
        trees.Add(InterpolatedExpressionTree.Verbatim(" }"));

        return InterpolatedExpressionTree.Concat(trees);
    }

    public override InterpolatedExpressionTree VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
        // Add parameters defined by this lambda expression to the set of evaluable parameters
        var evaluableSnapshot = _evaluableParameters;
        _evaluableParameters = _evaluableParameters.Add(node.Parameter.Identifier.Text);
        try {
            var lambda = InterpolatedExpressionTree.Lambda([Visit(node.Parameter)], Visit(node.Body));

            if(node.Modifiers.Count == 0)
                return lambda;

            return InterpolatedExpressionTree.Concat(
                InterpolatedExpressionTree.Verbatim(node.Modifiers.MkString("", m => m.Text, " ", " ")),
                lambda
            );
        } finally {
            _evaluableParameters = evaluableSnapshot;
        }
    }

    public override InterpolatedExpressionTree VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        // Add parameters defined by this lambda expression to the set of evaluable parameters
        var evaluableSnapshot = _evaluableParameters;
        _evaluableParameters = _evaluableParameters.Union(node.ParameterList.Parameters.Select(static p => p.Identifier.Text));
        try {
            var lambda = InterpolatedExpressionTree.Lambda(
                node.ParameterList.Parameters.Select(Visit).ToList(),
                Visit(node.Body)
            );

            if(node.Modifiers.Count == 0)
                return lambda;

            return InterpolatedExpressionTree.Concat(
                InterpolatedExpressionTree.Verbatim(node.Modifiers.MkString("", m => m.Text, " ", " ")),
                lambda
            );
        } finally {
            _evaluableParameters = evaluableSnapshot;
        }
    }

    public override InterpolatedExpressionTree VisitIdentifierName(IdentifierNameSyntax node) {
        var symbol = _context.SemanticModel.GetSymbolInfo(node).Symbol;
        if(symbol is not null && !TypeSymbolHelpers.IsAccessible(symbol))
            return _context.Diagnostics.InaccesibleSymbol(symbol, InterpolatedExpressionTree.Unsupported);

        if(symbol is IParameterSymbol && !_evaluableParameters.Contains(node.Identifier.Text)) {
            if(_interpolatableParameters.Contains(node.Identifier.Text))
                return _context.Diagnostics.EvaluatedParameter(node, InterpolatedExpressionTree.Unsupported);

            return _context.Diagnostics.Closure(node, InterpolatedExpressionTree.Unsupported);
        }

        return InterpolatedExpressionTree.Verbatim(node.Identifier.Text);
    }

    public override InterpolatedExpressionTree VisitParameter(ParameterSyntax node) {
        if(node.Type is null)
            return InterpolatedExpressionTree.Verbatim(node.Identifier.Text);

        if(_context.SemanticModel.GetTypeInfo(node.Type).Type is not {} parameterType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        if(!TypeSymbolHelpers.IsAccessible(parameterType))
            return _context.Diagnostics.InaccesibleSymbol(parameterType, InterpolatedExpressionTree.Unsupported);
        if(!TypeSymbolHelpers.TryCreateTypeName(parameterType, out var parameterTypeName))
            return _context.Diagnostics.UnsupportedType(parameterType, InterpolatedExpressionTree.Unsupported);

        return InterpolatedExpressionTree.Verbatim($"{parameterTypeName} {node.Identifier.Text}");
    }

    public override InterpolatedExpressionTree VisitBinaryExpression(BinaryExpressionSyntax node) =>
        InterpolatedExpressionTree.Binary(node.OperatorToken.ToString(), Visit(node.Left), Visit(node.Right));

    public override InterpolatedExpressionTree? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) =>
        InterpolatedExpressionTree.Concat(
            InterpolatedExpressionTree.Verbatim(node.OperatorToken.ToString()),
            Visit(node.Operand)
        );

    public override InterpolatedExpressionTree VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) =>
        InterpolatedExpressionTree.Concat(
            Visit(node.Operand),
            InterpolatedExpressionTree.Verbatim(node.OperatorToken.ToString())
        );

    public override InterpolatedExpressionTree VisitLiteralExpression(LiteralExpressionSyntax node) =>
        InterpolatedExpressionTree.Verbatim(node.ToFullString());
}
