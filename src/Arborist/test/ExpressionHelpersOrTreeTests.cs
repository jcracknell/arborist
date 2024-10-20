namespace Arborist;

public class ExpressionHelpersOrTreeTests {
    [Fact]
    public void OrTree_should_return_false_when_empty() {
        var expr = ExpressionHelpers.OrTree(Enumerable.Empty<Expression<Func<string, bool>>>());

        var constExpr = Assert.IsAssignableFrom<ConstantExpression>(expr.Body);
        Assert.Equal(false, constExpr.Value);
    }

    [Fact]
    public void OrTree_works_as_expected() {
        var expr = ExpressionHelpers.OrTree<Func<string, bool>>(
            x => true,
            x => false,
            x => false,
            x => true
        );

        var expectedBody = Expression.OrElse(
            Expression.OrElse(
                Expression.Constant(true),
                Expression.Constant(false)
            ),
            Expression.OrElse(
                Expression.Constant(false),
                Expression.Constant(true)
            )
        );

        Assert.Equivalent(expectedBody, expr.Body);
    }
}
