namespace Arborist;

public class ExpressionHelperReplaceTests {
    [Fact]
    public void Replace_should_work_as_expected() {
        var a = Expression.Constant(1);
        var b = Expression.Constant(2);
        var c = Expression.Constant(3);

        Assert.Equivalent(Expression.Add(a, c), ExpressionHelper.Replace(Expression.Add(a, b), b, c));
    }
}
