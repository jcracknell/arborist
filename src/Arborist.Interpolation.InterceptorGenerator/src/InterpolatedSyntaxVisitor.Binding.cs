namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitor {
    private InterpolatedExpressionBinding CurrentExpr { get; set; } = default!;

    public sealed class InterpolatedExpressionBinding(
        InterpolatedExpressionBinding? parent,
        InterpolatedSyntaxVisitor visitor,
        InterpolatedTree binding,
        Type? expressionType
    ) : ExpressionBinding(
        parent: parent,
        binding: binding,
        expressionType: expressionType
    ) {

        public InterpolatedTree Identifier { get; } = CreateIdentifier(parent?.Depth + 1 ?? 0);
        public bool IsMarked { get; private set; }

        private int Depth => (parent?.Depth + 1) ?? 0;

        private static InterpolatedTree CreateIdentifier(int depth) =>
            InterpolatedTree.Verbatim($"__e{depth}");

        protected override ExpressionBinding GetCurrent() {
            return visitor.CurrentExpr;
        }

        protected override void SetCurrent(ExpressionBinding? value) {
            visitor.CurrentExpr = (InterpolatedExpressionBinding)value!;
        }

        /// <summary>
        /// Marks the subject <see cref="ExpressionBinding"/>, signaling that the bound expression has been
        /// altered in some way in the result tree.
        /// </summary>
        public void Mark() {
            if(!IsMarked) {
                IsMarked = true;
                ((InterpolatedExpressionBinding?)Parent)?.Mark();
            }
        }

        /// <summary>
        /// Creates an <see cref="InterpolatedTree"/> referencing the descendant of the current expression tree node
        /// identified by the provided <paramref name="binding"/>.
        /// </summary>
        public InterpolatedTree BindValue(ref InterpolatedTree.InterpolationHandler binding) =>
            InterpolatedTree.Concat(Identifier, InterpolatedTree.Verbatim("."), binding.GetTree());

        protected override ExpressionBinding BindDescendant(Type? expressionType, ref InterpolatedTree.InterpolationHandler binding) =>
            new InterpolatedExpressionBinding(
                parent: this,
                visitor: visitor,
                binding: BindValue(ref binding),
                expressionType: expressionType
            );

        protected override InterpolatedTree CreateResult(InterpolatedTree value) {
            // If the provided value is unmarked, we return the binding directly to output the original
            // expression tree instead of the rewritten version of the tree
            if(!IsMarked && value.IsSupported)
                return Binding;

            if(ExpressionType is null) {
                if(value.IsSupported)
                    throw new InvalidOperationException($"Expression type is not set for body: {value}");

                return InterpolatedTree.Bind(Identifier, Binding, value);
            }

            return InterpolatedTree.Bind(
                Identifier,
                InterpolatedTree.Interpolate($"(global::{ExpressionType.FullName})({Binding})"),
                value
            );
        }
    }
}
