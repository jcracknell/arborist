namespace Arborist.Tests;

public partial class ExpressionHelpersTests {
    [Fact]
    public void Replace_should_work_as_expected() {
        var a = Expression.Constant(1);
        var b = Expression.Constant(2);
        var c = Expression.Constant(3);

        Assert.Equivalent(Expression.Add(a, c), ExpressionHelpers.Replace(Expression.Add(a, b), b, c));
    }
}
