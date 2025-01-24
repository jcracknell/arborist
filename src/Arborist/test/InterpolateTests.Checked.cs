using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_work_for_checked_numeric_conversion() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => checked((short)c.Id)),
            #pragma warning disable ARB003
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked((short)c.Id))
            #pragma warning restore
        );
    }

    [Fact]
    public void Should_work_for_checked_object_conversion() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => checked((object)c.Id)),
            #pragma warning disable ARB003
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked((object)c.Id))
            #pragma warning restore
        );
    }
}
