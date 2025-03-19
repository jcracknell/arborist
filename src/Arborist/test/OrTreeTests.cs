namespace Arborist;

public class OrTreeTests {
    [Fact]
    public void Should_return_false_when_empty() {
        var expr = ExpressionHelper.OrTree(Enumerable.Empty<Expression<Func<string, bool>>>());

        var constExpr = Assert.IsAssignableFrom<ConstantExpression>(expr.Body);
        Assert.Equal(false, constExpr.Value);
    }

    [Fact]
    public void OrTree_works_as_expected() {
        var expr = ExpressionHelper.OrTree([
            ExpressionOn<string>.Of(x => true),
            ExpressionOn<string>.Of(x => false),
            ExpressionOn<string>.Of(x => false),
            ExpressionOn<string>.Of(x => true)
        ]);

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

    [Fact]
    public void Should_be_left_biased() {
        var expr = ExpressionHelper.OrTree([
            ExpressionOn<string>.Of(x => true),
            ExpressionOn<string>.Of(x => false),
            ExpressionOn<string>.Of(x => true)
        ]);

        var expectedBody = Expression.OrElse(
            Expression.OrElse(
                Expression.Constant(true),
                Expression.Constant(false)
            ),
            Expression.Constant(true)
        );

        Assert.Equivalent(expectedBody, expr.Body);
    }

    [Fact]
    public void Should_throw_for_invalid_predicate_type() {
        Assert.Throws<InvalidOperationException>(() => {
            ExpressionHelper.OrTree(Enumerable.Empty<Expression<Action<object>>>());
        });
        Assert.Throws<InvalidOperationException>(() => {
            ExpressionHelper.OrTree(Enumerable.Empty<Expression<Func<object, string>>>());
        });
    }
}
