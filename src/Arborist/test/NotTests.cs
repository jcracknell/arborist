namespace Arborist;

public class NotTests {
    [Fact]
    public void Should_apply_unary_not() {
        var expr = ExpressionHelper.Not<Func<string, bool>>(x => true);

        var unary = Assert.IsAssignableFrom<UnaryExpression>(expr.Body);
        Assert.Equal(ExpressionType.Not, unary.NodeType);

        var constExpr = Assert.IsAssignableFrom<ConstantExpression>(unary.Operand);
        Assert.Equal(true, constExpr.Value);
    }

    [Fact]
    public void Should_do_nothing_special() {
        // N.B. the compiler will optimize !true in an expression tree
        var expr = ExpressionHelper.Not(ExpressionHelper.Not<Func<string, bool>>(x => true));

        var unary0 = Assert.IsAssignableFrom<UnaryExpression>(expr.Body);
        Assert.Equal(ExpressionType.Not, unary0.NodeType);

        var unary1 = Assert.IsAssignableFrom<UnaryExpression>(unary0.Operand);
        Assert.Equal(ExpressionType.Not, unary1.NodeType);

        var constExpr = Assert.IsAssignableFrom<ConstantExpression>(unary1.Operand);
        Assert.Equal(true, constExpr.Value);
    }

    [Fact]
    public void Should_throw_for_invalid_predicate_type() {
        Assert.Throws<InvalidOperationException>(() => {
            ExpressionHelper.Not(ExpressionOn<List<string>>.Of(l => l.Add("foo")));
        });
        Assert.Throws<InvalidOperationException>(() => {
            ExpressionHelper.AndTree(ExpressionOn<object>.Of(o => o.ToString()));
        });
    }
}
