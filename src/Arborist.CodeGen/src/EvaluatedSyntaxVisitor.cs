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

        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IFieldSymbol field when field.IsStatic || field.IsConst:
                return InterpolatedExpressionTree.Member(
                    InterpolatedExpressionTree.Verbatim(_builder.CreateTypeName(field.ContainingType)),
                    node.Name.ToString()
                );

            case IFieldSymbol field:
                return InterpolatedExpressionTree.Member(
                    Visit(node.Expression),
                    node.Name.ToString()
                );

            case IPropertySymbol { IsStatic: true } property:
                return InterpolatedExpressionTree.Member(
                    InterpolatedExpressionTree.Verbatim(_builder.CreateTypeName(property.ContainingType)),
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

        var methodName = GetMethodName(memberAccess.Name);

        switch(_context.SemanticModel.GetSymbolInfo(node).Symbol) {
            case IMethodSymbol method when method.IsStatic || method.IsExtensionMethod:
                return InterpolatedExpressionTree.StaticCall(
                    $"{_builder.CreateTypeName(method.ContainingType)}.{methodName}",
                    node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit).ToList()
                );

            case IMethodSymbol method:
                return InterpolatedExpressionTree.InstanceCall(
                    Visit(memberAccess.Expression),
                    methodName,
                    node.ArgumentList.Arguments.Select(static a => a.Expression).Select(Visit).ToList()
                );

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);
        }
    }

    private string GetMethodName(SimpleNameSyntax methodName) {
        // Emit type arguments if they were explicitly specified
        if(methodName is GenericNameSyntax generic)
            return generic.TypeArgumentList.Arguments.MkString($"{generic.Identifier.Text}<", GetTypeArgument, ", ", ">");

        return methodName.Identifier.Text;
    }

    private string GetTypeArgument(TypeSyntax node) {
        switch(_context.SemanticModel.GetTypeInfo(node).Type) {
            case INamedTypeSymbol namedType:
                return _builder.CreateTypeName(namedType);

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, "???");
        }
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
        if(_context.SemanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol namedType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);

        return InterpolatedExpressionTree.Concat(
            InterpolatedExpressionTree.Verbatim($"({_builder.CreateTypeName(namedType)})"),
            Visit(node.Expression)
        );
    }

    public override InterpolatedExpressionTree VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    public override InterpolatedExpressionTree VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    private InterpolatedExpressionTree VisitBaseObjectCreationExpression(BaseObjectCreationExpressionSyntax node) {
        var typeSymbol = _context.SemanticModel.GetTypeInfo(node).Type!;

        var newExpr = InterpolatedExpressionTree.StaticCall(
            node switch {
                ImplicitObjectCreationExpressionSyntax => "new",
                _ => $"new {_builder.CreateTypeName(typeSymbol)}"
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

        if(!_evaluableParameters.Contains(node.Identifier.Text)) {
            if(_interpolatableParameters.Contains(node.Identifier.Text))
                return _context.Diagnostics.EvaluatedParameter(node, InterpolatedExpressionTree.Unsupported);

            return _context.Diagnostics.Closure(node, InterpolatedExpressionTree.Unsupported);
        }

        return InterpolatedExpressionTree.Verbatim(node.Identifier.Text);
    }

    public override InterpolatedExpressionTree VisitParameter(ParameterSyntax node) {
        if(node.Type is null)
            return InterpolatedExpressionTree.Verbatim(node.Identifier.Text);

        if(_context.SemanticModel.GetTypeInfo(node.Type).Type is not INamedTypeSymbol namedType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node, InterpolatedExpressionTree.Unsupported);

        return InterpolatedExpressionTree.Verbatim($"{_builder.CreateTypeName(namedType)} {node.Identifier.Text}");
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
