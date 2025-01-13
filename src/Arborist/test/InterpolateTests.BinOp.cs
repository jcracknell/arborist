using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_work_for_i32_add() {
        #pragma warning disable ARB003
        Assert.Equivalent(
            expected: ExpressionOn<Owner>.Of(o => o.Id + 42),
            actual: ExpressionOn<Owner>.Interpolate((x, o) => o.Id + 42)
        );
        #pragma warning restore
    }

    [Fact]
    public void Should_work_for_i32_add_lifted() {
        #pragma warning disable ARB003
        #pragma warning disable CS0458
        Assert.Equivalent(
            expected: ExpressionOn<Owner>.Of(o => new int?(42) + null),
            actual: ExpressionOn<Owner>.Interpolate((x, o) => new int?(42) + null)
        );
        #pragma warning restore
    }

    [Fact]
    public void Should_work_for_i32_lt() {
        #pragma warning disable ARB003
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => c.Id < 42),
            actual: ExpressionOn<Cat>.Interpolate((x, c) => c.Id < 42)
        );
        #pragma warning restore
    }

    [Fact]
    public void Should_work_for_i32_lt_lifted() {
        #pragma warning disable ARB003
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => c.Age < 42),
            actual: ExpressionOn<Cat>.Interpolate((x, c) => c.Age < 42)
        );
        #pragma warning restore
    }

    [Fact]
    public void Should_work_for_string_add() {
        #pragma warning disable ARB003
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => c.Name + "bar"),
            actual: ExpressionOn<Cat>.Interpolate((x, c) => c.Name + "bar")
        );
        #pragma warning restore
    }
}
