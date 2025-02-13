using Arborist.TestFixtures;
using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_an_unquoted_lambda() {
        var interpolated = InterpolationTestOn<Owner>.Interpolate(
            default(object),
            (x, o) => o.CatsEnumerable.Any(c => c.Name == x.SpliceValue("Garfield"))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsEnumerable.Any(c => c.Name == "Garfield"));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_a_quoted_lambda() {
        var interpolated = InterpolationTestOn<Owner>.Interpolate(
            default(object),
            (x, o) => o.CatsQueryable.Any(c => c.Name == x.SpliceValue("Garfield"))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsQueryable.Any(c => c.Name == "Garfield"));

        Assert.Equivalent(expected, interpolated);
    }

}
