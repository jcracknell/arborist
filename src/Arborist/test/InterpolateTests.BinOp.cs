using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_work_for_i32_add() {
        Assert.Equivalent(
            expected: ExpressionOn<Owner>.Of(o => o.Id + 42),
            actual: ExpressionOn<Owner>.Interpolate(default(object), (x, o) => o.Id + x.SpliceConstant(42))
        );
    }

    [Fact]
    public void Should_work_for_i32_add_lifted() {
        #pragma warning disable CS0458
        Assert.Equivalent(
            expected: ExpressionOn<Owner>.Of(o => new int?(42) + null),
            actual: ExpressionOn<Owner>.Interpolate(default(object), (x, o) => new int?(x.SpliceConstant(42)) + null)
        );
        #pragma warning restore
    }

    [Fact]
    public void Should_work_for_i32_lt() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => c.Id < 42),
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Id < x.SpliceConstant(42))
        );
    }

    [Fact]
    public void Should_work_for_i32_lt_lifted() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => c.Age < 42),
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Age < x.SpliceConstant(42))
        );
    }

    [Fact]
    public void Should_work_for_string_add() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => c.Name + "bar"),
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Name + x.SpliceConstant("bar"))
        );
    }

    [Fact]
    public void Should_work_for_string_add_chained() {
        // This test validates that chained string addition is not optimized using a suitable
        // string.Concat overload
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => "bar" + c.Name + c.Name),
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => x.SpliceConstant("bar") + c.Name + c.Name)
        );
    }

    [Fact]
    public void Should_work_for_as() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => 42 as IFormattable),
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => x.SpliceConstant(42) as IFormattable)
        );
    }

    [Fact]
    public void Should_work_for_is() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => (object?)42 is IFormattable),
            actual: ExpressionOn<Cat>.Interpolate(default(object), (x, c) => (object?)x.SpliceConstant(42) is IFormattable)
        );
    }
}
