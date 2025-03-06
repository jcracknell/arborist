using Arborist.TestFixtures;
using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_an_unquoted_lambda() {
        var interpolated = InterpolationTestOn<Owner>.Interpolate(
            default(object),
            (x, o) => o.CatsEnumerable.Any(c => c.Name == x.SpliceConstant("Garfield"))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsEnumerable.Any(c => c.Name == "Garfield"));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_a_quoted_lambda() {
        var interpolated = InterpolationTestOn<Owner>.Interpolate(
            default(object),
            (x, o) => o.CatsQueryable.Any(c => c.Name == x.SpliceConstant("Garfield"))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsQueryable.Any(c => c.Name == "Garfield"));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_instance_call() {
        var interpolated = InterpolationTestOnNone.Interpolate(
            x => x.SpliceConstant(42).Equals(x.SpliceConstant(42))
        );

        var expected = ExpressionOnNone.Of(() => 42.Equals(42));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_static_call() {
        var interpolated = InterpolationTestOnNone.Interpolate(
            x => ImmutableList.Create(x.SpliceConstant(42))
        );

        var expected = ExpressionOnNone.Of(() => ImmutableList.Create(42));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_postfix_extension_method_call() {
        var interpolated = InterpolationTestOnNone.Interpolate(
            x => x.SpliceConstant("foo").Any(c => c.Equals(x.SpliceConstant('o')))
        );

        var expected = ExpressionOnNone.Of(() => "foo".Any(c => c.Equals('o')));

        Assert.Equivalent(expected, interpolated);
    }
}
