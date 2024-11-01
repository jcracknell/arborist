namespace Arborist;

public class ExpressionHelperAndTests {
    [Fact]
    public void And_should_return_true_when_empty() {
        var expr = ExpressionHelper.And(Enumerable.Empty<Expression<Func<string, bool>>>());

        var constExpr = Assert.IsAssignableFrom<ConstantExpression>(expr.Body);
        Assert.Equal((object)true, constExpr.Value);
    }

    [Fact]
    public void And_should_work_with_1_expression() {
        var expr = ExpressionHelper.And<Func<string, bool>>(x => true);

        var expected = Expression.Constant(true);

        Assert.Equivalent(expected, expr.Body);
    }

    [Fact]
    public void And_should_work_with_2_expressions() {
        var expr = ExpressionHelper.And<Func<string, bool>>(x => true, x => false);

        var expected = Expression.AndAlso(
            Expression.Constant(true),
            Expression.Constant(false)
        );

        Assert.Equivalent(expected, expr.Body);
    }

    [Fact]
    public void And_should_work_with_3_expressions() {
        var expr = ExpressionHelper.And<Func<string, bool>>(x => true, x => false, x => true);

        var expected = Expression.AndAlso(
            Expression.AndAlso(
                Expression.Constant(true),
                Expression.Constant(false)
            ),
            Expression.Constant(true)
        );

        Assert.Equivalent(expected, expr.Body);
    }
}
