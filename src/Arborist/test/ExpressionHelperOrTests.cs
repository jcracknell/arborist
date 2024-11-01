namespace Arborist;

public class ExpressionHelperOrTests {
    [Fact]
    public void Or_should_return_true_when_empty() {
        var expr = ExpressionHelper.Or(Enumerable.Empty<Expression<Func<string, bool>>>());

        var constExpr = Assert.IsAssignableFrom<ConstantExpression>(expr.Body);
        Assert.Equal(false, constExpr.Value);
    }

    [Fact]
    public void Or_should_work_with_1_expression() {
        var expr = ExpressionHelper.Or<Func<string, bool>>(x => true);

        var expected = Expression.Constant(true);

        Assert.Equivalent(expected, expr.Body);
    }

    [Fact]
    public void Or_should_work_with_2_expressions() {
        var expr = ExpressionHelper.Or<Func<string, bool>>(x => true, x => false);

        var expected = Expression.OrElse(
            Expression.Constant(true),
            Expression.Constant(false)
        );

        Assert.Equivalent(expected, expr.Body);
    }

    [Fact]
    public void Or_should_work_with_3_expressions() {
        var expr = ExpressionHelper.Or<Func<string, bool>>(x => true, x => false, x => true);

        var expected = Expression.OrElse(
            Expression.OrElse(
                Expression.Constant(true),
                Expression.Constant(false)
            ),
            Expression.Constant(true)
        );

        Assert.Equivalent(expected, expr.Body);
    }
}
