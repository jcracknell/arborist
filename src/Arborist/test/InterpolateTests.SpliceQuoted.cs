using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void SpliceQuoted_should_work_as_expected() {
        var data = new {
            Quoted = ExpressionOn<Cat>.Of(c => true)
        };

        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            o.CatsQueryable.Any(x.SpliceQuoted(x.Data.Quoted))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsQueryable.Any(c => true));

        Assert.Equivalent(expected, interpolated);
    }
}
