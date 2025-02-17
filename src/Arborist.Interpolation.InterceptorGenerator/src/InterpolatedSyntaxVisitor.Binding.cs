namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitor {
    private ExpressionBinding CurrentExpr { get; set; } = default!;

    private sealed class InterpolatedExpressionBinding : ExpressionBinding {
        private readonly InterpolatedSyntaxVisitor _visitor;

        public InterpolatedExpressionBinding(
            ExpressionBinding? parent,
            InterpolatedSyntaxVisitor visitor,
            string identifier,
            InterpolatedTree binding,
            Type? expressionType
        ) : base(
            parent: parent,
            identifierString: identifier,
            binding: binding,
            expressionType: expressionType
        ) {
            _visitor = visitor;
        }

        protected override ExpressionBinding GetCurrent() {
            return _visitor.CurrentExpr;
        }

        protected override void SetCurrent(ExpressionBinding? value) {
            _visitor.CurrentExpr = value!;
        }

        // If the tree is unmarked, we return the binding (the analyzed expression value)
        protected override InterpolatedTree GetUnmarkedValue(InterpolatedTree binding, InterpolatedTree value) =>
            binding;

        protected override ExpressionBinding Bind(string identifier, InterpolatedTree binding, Type? expressionType) =>
            new InterpolatedExpressionBinding(
                parent: this,
                visitor: _visitor,
                identifier: identifier,
                binding: binding,
                expressionType: expressionType
            );
    }
}
