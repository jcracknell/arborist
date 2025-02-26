using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitor {
    private EvaluatedExpressionBinding CurrentExpr { get; set; } = default!;

    // Expression binding on the evaluated side is a pain in the butt because (a) it's used
    // exclusively for captured references embedded in the expression tree, and (b) the elegant
    // approach of using switch expressions seen on the interpolated side will not work within
    // evaluated (literal) expression trees, so for uniformity we have to track all of the
    // capture references and evaluate them all at the top level using a giant mess of
    // chained casts.

    private abstract class EvaluatedExpressionBinding(
        EvaluatedSyntaxVisitor visitor,
        ExpressionBinding parent,
        InterpolatedTree binding,
        Type? expressionType
    ) : ExpressionBinding(
        parent: parent,
        binding: binding,
        expressionType: expressionType
    ) {
        protected static readonly InterpolatedTree BindingPlaceholder = InterpolatedTree.Placeholder("BINDING");

        public ImmutableList<KeyValuePair<string, InterpolatedTree>> CapturedValueBindings { get; set; } =
            ImmutableList<KeyValuePair<string, InterpolatedTree>>.Empty;

        public abstract string CreateCapturedValueIdentifier();

        protected override ExpressionBinding GetCurrent() {
            return visitor.CurrentExpr;
        }

        protected override void SetCurrent(ExpressionBinding? value) {
            visitor.CurrentExpr = (EvaluatedExpressionBinding)value!;
        }

        /// <summary>
        /// Binds the value of the embedded constant represented by the current node of the expression tree
        /// with the specified <paramref name="type"/>.
        /// </summary>
        public InterpolatedTree BindCapturedConstant(ITypeSymbol type, SyntaxNode? node) {
            SetType(typeof(ConstantExpression));

            var nullForgiveness = NullableAnnotation.NotAnnotated == type.NullableAnnotation ? "!" : "";
            var constantValueTree = InterpolatedTree.Interpolate($"{BindingPlaceholder}.{nameof(ConstantExpression.Value)}{nullForgiveness}");

            return BindCapturedValue(type, node, constantValueTree);
        }

        /// <summary>
        /// Binds the value of the captured local variable represented by the current node of the expression
        /// tree with the specified <paramref name="type"/>.
        /// </summary>
        public InterpolatedTree BindCapturedLocal(ITypeSymbol type, SyntaxNode? node) {
            SetType(typeof(MemberExpression));

            var nullForgiveness = NullableAnnotation.NotAnnotated == type.NullableAnnotation ? "!" : "";
            var helperCall = InterpolatedTree.Call(
                InterpolatedTree.Verbatim("global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue"),
                [BindingPlaceholder]
            );

            return BindCapturedValue(type, node, InterpolatedTree.Interpolate($"{helperCall}{nullForgiveness}"));
        }

        private InterpolatedTree BindCapturedValue(ITypeSymbol type, SyntaxNode? node, InterpolatedTree tree) {
            var builder = visitor._builder;

            var binding = new KeyValuePair<string, InterpolatedTree>(
                key: CreateCapturedValueIdentifier(),
                value: builder.TryCreateTypeName(type, out var typeName) switch {
                    // If the type is nameable, then by default we'll create a tree-shaped cast
                    true => InterpolatedTree.CastTree(typeName, tree),
                    // Otherwise attempt to create a TypeRef to perform the cast
                    false => InterpolatedTree.Call(
                        InterpolatedTree.Interpolate($"{builder.CreateTypeRef(type)}.Cast"),
                        [tree]
                    )
                }
            );

            CapturedValueBindings = CapturedValueBindings.Add(binding);
            return InterpolatedTree.Verbatim(binding.Key);
        }

        protected override ExpressionBinding BindDescendant(Type? expressionType, ref InterpolatedTree.InterpolationHandler binding) =>
            new DescendantEvaluatedExpressionBinding(
                visitor: visitor,
                parent: this,
                binding: binding.GetTree(),
                expressionType: expressionType
            );
    }

    private class RootEvaluatedExpressionBinding(
        EvaluatedSyntaxVisitor visitor,
        InterpolatedSyntaxVisitor.InterpolatedExpressionBinding parent,
        InterpolatedTree binding
    ) : EvaluatedExpressionBinding(
        visitor: visitor,
        parent: parent,
        binding: binding,
        expressionType: default
    ) {
        private int _constantIdentifierCount = 0;

        public override string CreateCapturedValueIdentifier() {
            var identifier = $"__c{_constantIdentifierCount}";
            _constantIdentifierCount += 1;
            return identifier;
        }

        public override void SetType(Type type) {
            parent.SetType(type);
            base.SetType(type);
        }

        protected override void SetCurrent(ExpressionBinding? value) {
            if(ReferenceEquals(Parent, value))
                return;

            base.SetCurrent(value);
        }

        protected override InterpolatedTree CreateResult(InterpolatedTree value) =>
            InterpolatedTree.BindTuple(
                CapturedValueBindings.SelectEager(b => new KeyValuePair<string, InterpolatedTree>(
                    key: b.Key,
                    value: b.Value.Replace(BindingPlaceholder, parent.Identifier)
                )),
                value
            );
    }

    private class DescendantEvaluatedExpressionBinding(
        EvaluatedSyntaxVisitor visitor,
        EvaluatedExpressionBinding parent,
        InterpolatedTree binding,
        Type? expressionType
    ) : EvaluatedExpressionBinding(
        visitor: visitor,
        parent: parent,
        binding: binding,
        expressionType: expressionType
    ) {
        public override string CreateCapturedValueIdentifier() =>
            parent.CreateCapturedValueIdentifier();

        protected override InterpolatedTree CreateResult(InterpolatedTree value) {
            // Pass our bindings up to the parent expression now that we know the type of the current binding
            PropagateCapturedValueBindings();
            return value;
        }

        private void PropagateCapturedValueBindings() {
            var replacement = ExpressionType switch {
                null => InterpolatedTree.Interpolate($"{BindingPlaceholder}.{Binding}"),
                not null => InterpolatedTree.CastTree(
                    InterpolatedTree.Interpolate($"global::{ExpressionType.FullName}"),
                    InterpolatedTree.Interpolate($"{BindingPlaceholder}.{Binding}")
                )
            };

            foreach(var binding in CapturedValueBindings)
                parent.CapturedValueBindings = parent.CapturedValueBindings.Add(new(
                    key: binding.Key,
                    value: binding.Value.Replace(BindingPlaceholder, replacement)
                ));
        }
    }
}
