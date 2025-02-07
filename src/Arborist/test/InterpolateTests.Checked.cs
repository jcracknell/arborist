using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_work_for_checked_numeric_conversion() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => checked((short)(c.Id + 42))),
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked((short)(c.Id + x.SpliceValue(42))))
        );
    }

    [Fact]
    public void Should_work_for_checked_object_conversion() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => checked((object)(c.Id + 42))),
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked((object)(c.Id + x.SpliceValue(42))))
        );
    }
}
