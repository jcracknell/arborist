using Arborist.TestFixtures;

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

    [Fact]
    public void SpliceConstant_should_work_with_implicit_conversion() {
        var interpolated = ExpressionOnNone.Interpolate(x => x.SpliceConstant<ImplicitlyConvertible<int>>(42));

        var expected = ExpressionOnNone.Constant(new ImplicitlyConvertible<int>(42));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceConstant_should_work_with_numeric_conversion() {
        var interpolated = ExpressionOnNone.Interpolate(
            new { Value = 42 },
            x => x.SpliceConstant<long>(x.Data.Value)
        );

        var expected = ExpressionOnNone.Constant(42L);

        Assert.Equivalent(expected, interpolated);
    }
}
