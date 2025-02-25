using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitor : CSharpSyntaxVisitor<InterpolatedTree> {
    private readonly InterpolationAnalysisContext _context;
    private readonly InterpolatedTreeBuilder _builder;
    private QueryContext _queryContext;

    /// <summary>
    /// Identifiers defined in the interpolated tree which cannot be referenced in an evaluated
    /// expression (as they are not defined at evaluation time).
    /// </summary>
    private readonly ImmutableHashSet<string> _interpolatedIdentifiers;

    /// <summary>
    /// Mapping of identifiers defined and referenceable in the evaluated tree to their
    /// referencing expressions (a query identifier maps to a property reference)
    /// </summary>
    private ImmutableDictionary<string, InterpolatedTree> _evaluableIdentifiers;

    public EvaluatedSyntaxVisitor(
        InterpolationAnalysisContext context,
        InterpolatedSyntaxVisitor.InterpolatedExpressionBinding currentExpr,
        ImmutableHashSet<string> interpolatedIdentifiers
    ) {
        _context = context;
        _builder = context.TreeBuilder;
        _interpolatedIdentifiers = interpolatedIdentifiers;
        _evaluableIdentifiers = ImmutableDictionary.Create<string, InterpolatedTree>(IdentifierEqualityComparer.Instance);
        _queryContext = QueryContext.Create(this);

        CurrentExpr = new RootEvaluatedExpressionBinding(
            parent: currentExpr,
            visitor: this,
            binding: InterpolatedTree.Unsupported
        );
    }

    public InterpolatedTree Apply(SyntaxNode node) =>
        CurrentExpr.WithValue(Visit(node));

    public override InterpolatedTree Visit(SyntaxNode? node) {
        // Check for cancellation every time we visit (iterate) over a node
        _context.CancellationToken.ThrowIfCancellationRequested();

        if(node is null || !SyntaxHelpers.HasImplicitConversion(node, _context.SemanticModel))
            return base.Visit(node)!;

        CurrentExpr.SetType(typeof(UnaryExpression));
        return CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(base.Visit(node)!);
    }

    public override InterpolatedTree DefaultVisit(SyntaxNode node) {
        return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
    }

    public override InterpolatedTree VisitConditionalExpression(ConditionalExpressionSyntax node) {
        CurrentExpr.SetType(typeof(ConditionalExpression));
        return InterpolatedTree.Ternary(
            CurrentExpr.Bind($"{nameof(ConditionalExpression.Test)}").WithValue(Visit(node.Condition)),
            CurrentExpr.Bind($"{nameof(ConditionalExpression.IfTrue)}").WithValue(Visit(node.WhenTrue)),
            CurrentExpr.Bind($"{nameof(ConditionalExpression.IfFalse)}").WithValue(Visit(node.WhenFalse))
        );
    }

    public override InterpolatedTree VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        switch(methodSymbol) {
            // If this is an extension method invoked as a postfix method, then we need to rewrite the call
            // as a static method invocation so we don't have to import the extension into scope.
            case { ReducedFrom: { } }:
                if(!SymbolHelpers.IsAccessibleFromInterceptor(methodSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(methodSymbol, node);

                // A postfix extension method invocation should always be MemberAccessExpressionSyntax
                // of the form subject.Extension(...)
                if(node.Expression is not MemberAccessExpressionSyntax memberAccess)
                    return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

                var extensionTypeName = _builder.CreateTypeName(methodSymbol.ContainingType, node);
                var methodName = GetInvocationMethodName(node, memberAccess.Name);

                CurrentExpr.SetType(typeof(MethodCallExpression));
                return InterpolatedTree.Call(
                    InterpolatedTree.Interpolate($"{extensionTypeName}.{methodName}"),
                    [
                        CurrentExpr.BindCallArg(methodSymbol, 0).WithValue(Visit(memberAccess.Expression)),
                        ..node.ArgumentList.Arguments.SelectEager(
                            (a, i) => CurrentExpr.BindCallArg(methodSymbol, i + 1).WithValue(Visit(a.Expression))
                        )
                    ]
                );

            case { IsStatic: true }:
                CurrentExpr.SetType(typeof(MethodCallExpression));
                return InterpolatedTree.Call(
                    // Method/type name syntax does not have a bound expression
                    Visit(node.Expression),
                    node.ArgumentList.Arguments.SelectEager(
                        (a, i) => CurrentExpr.BindCallArg(methodSymbol, i).WithValue(Visit(a.Expression))
                    )
                );

            default:
                CurrentExpr.SetType(typeof(MethodCallExpression));
                return InterpolatedTree.Call(
                    CurrentExpr.BindCallArg(methodSymbol, 0).WithValue(Visit(node.Expression)),
                    node.ArgumentList.Arguments.SelectEager(
                        (a, i) => CurrentExpr.BindCallArg(methodSymbol, i + 1).WithValue(Visit(a.Expression))
                    )
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

    public override InterpolatedTree VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
        // Replace references to the data property of the interpolation context with the
        // locally defined data reference.
        if(_context.IsInterpolationDataAccess(node)) {
            CurrentExpr.SetType(typeof(MemberExpression));
            return InterpolatedTree.Verbatim(_builder.DataIdentifier);
        }

        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not {} symbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        switch(symbol) {
            case IFieldSymbol fieldSymbol:
                if(!SymbolHelpers.IsAccessibleFromInterceptor(fieldSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(fieldSymbol, node);

                var fieldObjectTree = fieldSymbol.IsStatic switch {
                    true => _builder.CreateTypeName(fieldSymbol.ContainingType, node),
                    false => CurrentExpr.Bind($"{nameof(MemberExpression.Expression)}!").WithValue(Visit(node.Expression))
                };

                CurrentExpr.SetType(typeof(MemberExpression));
                return InterpolatedTree.Interpolate($"{fieldObjectTree}.{fieldSymbol.Name}");

            case IPropertySymbol propertySymbol:
                if(!SymbolHelpers.IsAccessibleFromInterceptor(propertySymbol))
                    return _context.Diagnostics.InaccessibleSymbol(propertySymbol, node);

                var propertyObjectTree = propertySymbol.IsStatic switch {
                    true => _builder.CreateTypeName(propertySymbol.ContainingType, node),
                    false => CurrentExpr.Bind($"{nameof(MemberExpression.Expression)}!").WithValue(Visit(node.Expression))
                };

                CurrentExpr.SetType(typeof(MemberExpression));
                return InterpolatedTree.Interpolate($"{propertyObjectTree}.{propertySymbol.Name}");

            case IMethodSymbol methodSymbol:
                if(!SymbolHelpers.IsAccessibleFromInterceptor(methodSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(methodSymbol, node);

                var methodName = GetInvocationMethodName(node.Parent, node.Name);
                var methodObjectTree = methodSymbol.IsStatic switch {
                    true => _builder.CreateTypeName(methodSymbol.ContainingType, node),
                    false => CurrentExpr.Bind($"{nameof(MemberExpression.Expression)}!").WithValue(Visit(node.Expression))
                };

                return InterpolatedTree.Interpolate($"{methodObjectTree}.{methodName}");

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
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
                if(!SymbolHelpers.IsAccessibleFromInterceptor(fieldSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(symbol, node);

                var fieldObjectTree = fieldSymbol.IsStatic switch {
                    true => _builder.CreateTypeName(fieldSymbol.ContainingType, node),
                    false => CurrentExpr.Bind($"{nameof(MemberExpression.Expression)}!")
                    .WithValue(CurrentExpr.BindCapturedConstant(fieldSymbol.ContainingType, node))
                };

                CurrentExpr.SetType(typeof(MemberExpression));
                return InterpolatedTree.Interpolate($"{fieldObjectTree}.{fieldSymbol.Name}");

            case IPropertySymbol propertySymbol:
                if(!SymbolHelpers.IsAccessibleFromInterceptor(propertySymbol))
                    return _context.Diagnostics.InaccessibleSymbol(symbol, node);

                var propertyObjectTree = propertySymbol.IsStatic switch {
                    true => _builder.CreateTypeName(propertySymbol.ContainingType, node),
                    false => CurrentExpr.Bind($"{nameof(MemberExpression.Expression)}!")
                    .WithValue(CurrentExpr.BindCapturedConstant(propertySymbol.ContainingType, node))
                };

                CurrentExpr.SetType(typeof(MemberExpression));
                return InterpolatedTree.Interpolate($"{propertyObjectTree}.{propertySymbol.Name}");

            case IMethodSymbol methodSymbol:
                if(!SymbolHelpers.IsAccessibleFromInterceptor(methodSymbol))
                    return _context.Diagnostics.InaccessibleSymbol(symbol, node);

                var methodName = GetInvocationMethodName(node.Parent, node);
                var methodObjectTree = methodSymbol.IsStatic switch {
                    true => _builder.CreateTypeName(methodSymbol.ContainingType, node),
                    false => CurrentExpr.Bind($"{nameof(MemberExpression.Expression)}!")
                    .WithValue(CurrentExpr.BindCapturedConstant(methodSymbol.ContainingType, node))
                };

                return InterpolatedTree.Interpolate($"{methodObjectTree}.{methodName}");

            default:
                // Check evaluable identifiers first in case the identifier shadows an interpolated one
                if(_evaluableIdentifiers.TryGetValue(node.Identifier.ValueText, out var mappedTree)) {
                    CurrentExpr.SetType(typeof(Expression));
                    return mappedTree;
                }

                if(_interpolatedIdentifiers.Contains(node.Identifier.ValueText))
                    return _context.Diagnostics.EvaluatedInterpolatedIdentifier(node);

                if(_context.SemanticModel.GetTypeInfo(node).Type is not {} nodeType)
                    return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

                return CurrentExpr.BindCapturedLocal(nodeType, node);
        }
    }

    public override InterpolatedTree VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) {
        CurrentExpr.SetType(typeof(NewExpression));
        return InterpolatedTree.AnonymousClass(node.Initializers.SelectEager(
            (init, i) => CurrentExpr.Bind($"{nameof(NewExpression.Arguments)}[{i}]").WithValue(Visit(init))
        ));
    }

    public override InterpolatedTree VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node) =>
        node.NameEquals switch {
            null => Visit(node.Expression),
            not null => InterpolatedTree.Interpolate($"{node.NameEquals.Name.Identifier.ValueText} = {Visit(node.Expression)}")
        };

    public override InterpolatedTree VisitCheckedExpression(CheckedExpressionSyntax node) {
        // Checked/unchecked expressions do not appear in the expression tree.
        return InterpolatedTree.Call(InterpolatedTree.Verbatim(node.Keyword.ValueText), [
            Visit(node.Expression)
        ]);
    }

    public override InterpolatedTree VisitDefaultExpression(DefaultExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        CurrentExpr.SetType(typeof(ConstantExpression));
        return _builder.CreateDefaultValue(typeSymbol.WithNullableAnnotation(NullableAnnotation.Annotated));
    }

    public override InterpolatedTree VisitCastExpression(CastExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} nodeType)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        var typeName = _builder.CreateTypeName(nodeType, node);
        var operandTree = CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(node.Expression));

        CurrentExpr.SetType(typeof(UnaryExpression));
        return InterpolatedTree.Interpolate($"({typeName}){operandTree}");
    }

    public override InterpolatedTree VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    public override InterpolatedTree VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) =>
        VisitBaseObjectCreationExpression(node);

    private InterpolatedTree VisitBaseObjectCreationExpression(BaseObjectCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        switch(node.Initializer?.Kind()) {
            case null:
                return VisitBaseObjectCreationExpressionCore(node, typeSymbol);

            case SyntaxKind.ObjectInitializerExpression:
                CurrentExpr.SetType(typeof(MemberInitExpression));
                return InterpolatedTree.Concat(
                    CurrentExpr.Bind($"{nameof(MemberInitExpression.NewExpression)}")
                    .WithValue(VisitBaseObjectCreationExpressionCore(node, typeSymbol)),
                    InterpolatedTree.Verbatim(" "),
                    Visit(node.Initializer)
                );

            case SyntaxKind.CollectionInitializerExpression:
                CurrentExpr.SetType(typeof(ListInitExpression));
                return InterpolatedTree.Concat(
                    CurrentExpr.Bind($"{nameof(ListInitExpression.NewExpression)}")
                    .WithValue(VisitBaseObjectCreationExpressionCore(node, typeSymbol)),
                    InterpolatedTree.Verbatim(" "),
                    Visit(node.Initializer)
                );

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        }
    }

    private InterpolatedTree VisitBaseObjectCreationExpressionCore(BaseObjectCreationExpressionSyntax node, ITypeSymbol typeSymbol) {
        if(_context.SemanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol methodSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        if(!SymbolHelpers.IsAccessibleFromInterceptor(methodSymbol))
            return _context.Diagnostics.InaccessibleSymbol(methodSymbol, node);

        CurrentExpr.SetType(typeof(NewExpression));
        return InterpolatedTree.Call(
            node switch {
                ImplicitObjectCreationExpressionSyntax => InterpolatedTree.Verbatim("new"),
                _ => InterpolatedTree.Interpolate($"new {_builder.CreateTypeName(typeSymbol, node)}")
            },
            node.ArgumentList switch {
                null => Array.Empty<InterpolatedTree>(),
                not null => node.ArgumentList.Arguments.SelectEager(
                    (a, i) => CurrentExpr.Bind($"{nameof(NewExpression.Arguments)}[{i}]").WithValue(Visit(a.Expression))
                )
            }
        );
    }

    public override InterpolatedTree VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) {
        CurrentExpr.SetType(typeof(NewArrayExpression));
        return InterpolatedTree.Concat(
            InterpolatedTree.Verbatim("new[] "),
            InterpolatedTree.Initializer(node.Initializer.Expressions.SelectEager(
                (expr, i) => CurrentExpr.Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(expr))
            ))
        );
    }

    public override InterpolatedTree VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not IArrayTypeSymbol typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        var elementType = SymbolHelpers.GetRootArrayElementType(typeSymbol);
        if(!_builder.TryCreateTypeName(elementType, out var elementTypeName))
            return _context.Diagnostics.UnsupportedType(typeSymbol, node);

        CurrentExpr.SetType(typeof(NewArrayExpression));

        // Multi-dimensional array initializers are forbidden in expression trees, so if there is
        // an initializer then it's an implicit single-dimensional array
        if(node.Initializer is not null)
            return InterpolatedTree.Concat(
                InterpolatedTree.Interpolate($"new {elementTypeName}[] "),
                InterpolatedTree.Initializer(node.Initializer.Expressions.SelectEager(
                    (expr, i) => CurrentExpr.Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(expr))
                ))
            );

        // Otherwise the array dimensions are not required to be constants, and the expression is a
        // NewArrayBounds. Note that only the first rank specifier of the array can contain dimensions
        // (if there are multiple specifiers it is a nested array type).
        var sizeTrees = new List<InterpolatedTree>(2 * node.Type.RankSpecifiers[0].Sizes.Count - 1);
        foreach(var (size, i) in node.Type.RankSpecifiers[0].Sizes.ZipWithIndex()) {
            if(i != 0)
                sizeTrees.Add(InterpolatedTree.Verbatim(", "));

            sizeTrees.Add(CurrentExpr.Bind($"{nameof(NewArrayExpression.Expressions)}[{i}]").WithValue(Visit(size)));
        }

        var nested = Enumerable.Repeat("[]", node.Type.RankSpecifiers.Count - 1).MkString("");
        return InterpolatedTree.Interpolate($"new {elementTypeName}[{InterpolatedTree.Concat(sizeTrees)}]{nested}");
    }

    public override InterpolatedTree VisitInitializerExpression(InitializerExpressionSyntax node) {
        switch(node.Kind()) {
            case SyntaxKind.ObjectInitializerExpression:
                // N.B. ListInitExpression and MemberListBinding both have the Initializers property, so it
                // does not matter which case this is.
                return InterpolatedTree.Initializer(node.Expressions.SelectEager(
                    (init, i) => CurrentExpr.Bind($"{nameof(MemberInitExpression.Bindings)}[{i}]")
                    .WithValue(VisitObjectInitializerElement(init))
                ));

            case SyntaxKind.CollectionInitializerExpression:
                // N.B. ListInitExpression and MemberListBinding both have the Initializers property, so it
                // does not matter which case this is.
                return InterpolatedTree.Initializer(node.Expressions.SelectEager(
                    (e, i) => CurrentExpr.Bind($"{nameof(ListInitExpression.Initializers)}[{i}]")
                    .WithValue(VisitCollectionInitializerElement(e))
                ));

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        }
    }

    private InterpolatedTree VisitObjectInitializerElement(ExpressionSyntax node) {
        if(node is not AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier: var identifier } } assignment)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        // Handle the case of a nested object/collection initializer
        if(assignment.Right is InitializerExpressionSyntax initializer) switch(initializer.Kind()) {
            case SyntaxKind.ObjectInitializerExpression:
                CurrentExpr.SetType(typeof(MemberMemberBinding));
                return InterpolatedTree.Interpolate($"{identifier.ValueText} = {VisitInitializerExpression(initializer)}");

            case SyntaxKind.CollectionInitializerExpression:
                CurrentExpr.SetType(typeof(MemberListBinding));
                return InterpolatedTree.Interpolate($"{identifier.ValueText} = {VisitInitializerExpression(initializer)}");

            default:
                return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);
        }

        CurrentExpr.SetType(typeof(MemberAssignment));

        var valueTree = CurrentExpr.Bind($"{nameof(MemberAssignment.Expression)}")
        .WithValue(Visit(assignment.Right));

        return InterpolatedTree.Interpolate($"{identifier.ValueText} = {valueTree}");
    }

    private InterpolatedTree VisitCollectionInitializerElement(ExpressionSyntax node) {
        CurrentExpr.SetType(typeof(ElementInit));
        switch(node) {
            case InitializerExpressionSyntax initializer:
                return InterpolatedTree.Initializer(initializer.Expressions.SelectEager(
                    (e, i) => CurrentExpr.Bind($"{nameof(ElementInit.Arguments)}[{i}]").WithValue(Visit(e))
                ));

            default:
                return CurrentExpr.Bind($"{nameof(ElementInit.Arguments)}[0]").WithValue(Visit(node));
        }
    }

    public override InterpolatedTree VisitAssignmentExpression(AssignmentExpressionSyntax node) =>
        InterpolatedTree.Interpolate($"{Visit(node.Left)} = {Visit(node.Right)}");

    public override InterpolatedTree VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) =>
        VisitLambdaExpression(node, new[] { node.Parameter });

    public override InterpolatedTree VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) =>
        VisitLambdaExpression(node, node.ParameterList.Parameters);

    private InterpolatedTree VisitLambdaExpression(LambdaExpressionSyntax node, IReadOnlyList<ParameterSyntax> parameters) {
        if(_context.SemanticModel.GetTypeInfo(node).ConvertedType is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        if(!SymbolHelpers.IsSubtype(typeSymbol, _context.TypeSymbols.Expression))
            return VisitLambdaExpressionCore(node, parameters);

        CurrentExpr.SetType(typeof(UnaryExpression));
        return CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(VisitLambdaExpressionCore(node, parameters));
    }

    private InterpolatedTree VisitLambdaExpressionCore(LambdaExpressionSyntax node, IReadOnlyList<ParameterSyntax> parameters) {
        var snapshot = _evaluableIdentifiers;
        try {
            var parameterTrees = new List<InterpolatedTree>(parameters.Count);
            foreach(var (parameter, i) in parameters.ZipWithIndex()) {
                _evaluableIdentifiers = _evaluableIdentifiers.SetItem(
                    parameter.Identifier.ValueText,
                    InterpolatedTree.Verbatim(parameter.Identifier.ValueText)
                );

                parameterTrees.Add(
                    CurrentExpr.Bind($"{nameof(LambdaExpression.Parameters)}[{i}]")
                    .WithValue(Visit(parameter))
                );
            }

            CurrentExpr.SetType(typeof(LambdaExpression));
            return InterpolatedTree.Lambda(
                parameterTrees,
                CurrentExpr.Bind($"{nameof(LambdaExpression.Body)}").WithValue(Visit(node.Body))
            );
        } finally {
            _evaluableIdentifiers = snapshot;
        }
    }

    public override InterpolatedTree VisitThisExpression(ThisExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        return CurrentExpr.BindCapturedConstant(typeSymbol, node);
    }

    public override InterpolatedTree VisitParameter(ParameterSyntax node) {
        CurrentExpr.SetType(typeof(ParameterExpression));

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

        CurrentExpr.SetType(typeof(BinaryExpression));
        return InterpolatedTree.Binary(
            node.OperatorToken.ToString(),
            CurrentExpr.Bind($"{nameof(BinaryExpression.Left)}").WithValue(Visit(node.Left)),
            CurrentExpr.Bind($"{nameof(BinaryExpression.Right)}").WithValue(Visit(node.Right))
        );
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

        CurrentExpr.SetType(typeof(UnaryExpression));
        return InterpolatedTree.Binary(
            node.OperatorToken.ToString(),
            CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(node.Left)),
            _builder.CreateTypeName(typeOperand, node.Right)
        );
    }

    private InterpolatedTree VisitBinaryIsExpression(BinaryExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node.Right).Type is not {} typeOperand)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node.Right);

        CurrentExpr.SetType(typeof(TypeBinaryExpression));
        return InterpolatedTree.Binary(
            node.OperatorToken.ToString(),
            CurrentExpr.Bind($"{nameof(TypeBinaryExpression.Expression)}").WithValue(Visit(node.Left)),
            _builder.CreateTypeName(typeOperand, node.Right)
        );
    }

    public override InterpolatedTree VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {
        if(TryVisitUnarySpecialExpression(node, node.Operand, out var special))
            return special;

        CurrentExpr.SetType(typeof(UnaryExpression));
        return InterpolatedTree.Concat(
            InterpolatedTree.Verbatim(node.OperatorToken.ToString()),
            CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(node.Operand))
        );
    }

    public override InterpolatedTree VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) {
        if(TryVisitUnarySpecialExpression(node, node.Operand, out var special))
            return special;

        CurrentExpr.SetType(typeof(UnaryExpression));
        return InterpolatedTree.Concat(
            CurrentExpr.Bind($"{nameof(UnaryExpression.Operand)}").WithValue(Visit(node.Operand)),
            InterpolatedTree.Verbatim(node.OperatorToken.ToString())
        );
    }

    private bool TryVisitUnarySpecialExpression(
        ExpressionSyntax node,
        ExpressionSyntax operand,
        [NotNullWhen(true)] out InterpolatedTree? result
    ) {
        switch(node.Kind()) {
            case SyntaxKind.SuppressNullableWarningExpression:
                // The null forgiving operator does not appear in the expression tree, so we apply it in
                // the result expression and proceed to visit the descendant operator without rebinding
                // the expression
                result = InterpolatedTree.Interpolate($"{Visit(operand)}!");
                return true;
            default:
                result = default;
                return false;
        }
    }

    public override InterpolatedTree VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) =>
        InterpolatedTree.Interpolate($"({Visit(node.Expression)})");

    public override InterpolatedTree VisitLiteralExpression(LiteralExpressionSyntax node) {
        CurrentExpr.SetType(typeof(ConstantExpression));
        return node.Kind() switch {
            SyntaxKind.DefaultLiteralExpression => VisitDefaultLiteralExpression(node),
            _ => InterpolatedTree.Verbatim(node.ToString().Trim())
        };
    }

    private InterpolatedTree VisitDefaultLiteralExpression(LiteralExpressionSyntax node) {
        if(_context.SemanticModel.GetTypeInfo(node).Type is not {} typeSymbol)
            return _context.Diagnostics.UnsupportedEvaluatedSyntax(node);

        CurrentExpr.SetType(typeof(ConstantExpression));
        return _builder.CreateDefaultValue(typeSymbol.WithNullableAnnotation(NullableAnnotation.Annotated));
    }
}
