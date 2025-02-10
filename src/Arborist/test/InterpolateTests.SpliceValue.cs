namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void SpliceValue_should_work_as_expected() {
        var interpolated = InterpolationTestOnNone.Interpolate(
            default(object),
            x => x.SpliceValue("foo")
        );

        var expected = Expression.Lambda<Func<string>>(Expression.Constant("foo"));

        Assert.Equivalent(expected, interpolated);
    }
}
