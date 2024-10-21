namespace Arborist;

public class ExpressionHelpersAndTreeTests {
    [Fact]
    public void AndTree_should_return_true_when_empty() {
        var expr = ExpressionHelpers.AndTree(Enumerable.Empty<Expression<Func<string, bool>>>());

        var constExpr = Assert.IsAssignableFrom<ConstantExpression>(expr.Body);
        Assert.Equal(true, constExpr.Value);
    }

    [Fact]
    public void AndTree_works_as_expected() {
        var expr = ExpressionHelpers.AndTree<Func<string, bool>>(
            x => true,
            x => false,
            x => false,
            x => true
        );

        var expectedBody = Expression.AndAlso(
            Expression.AndAlso(
                Expression.Constant(true),
                Expression.Constant(false)
            ),
            Expression.AndAlso(
                Expression.Constant(false),
                Expression.Constant(true)
            )
        );

        Assert.Equivalent(expectedBody, expr.Body);
    }

    [Fact]
    public void AndTree_should_be_left_biased() {
        var expr = ExpressionHelpers.AndTree<Func<string, bool>>(
            x => true,
            x => false,
            x => true
        );

        var expectedBody = Expression.AndAlso(
            Expression.AndAlso(
                Expression.Constant(true),
                Expression.Constant(false)
            ),
            Expression.Constant(true)
        );

        Assert.Equivalent(expectedBody, expr.Body);
    }
}
