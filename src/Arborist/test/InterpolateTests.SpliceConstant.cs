namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void SpliceConstant_should_work_as_expected() {
        var interpolated = ExpressionOnNone.Interpolate(
            default(object),
            x => x.SpliceConstant("foo")
        );

        var expected = Expression.Lambda<Func<string>>(Expression.Constant("foo"));

        Assert.Equivalent(expected, interpolated);
    }
}
