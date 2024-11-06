using Arborist.Fixtures;

namespace Arborist;

public class ReadmeTests {
    [Fact]
    public void Introductory_example_should_work() {
        var dogPredicate = ExpressionOn<Dog>.Of(d => d.Name == "Odie");

        var ownerPredicate = ExpressionOn<Owner>.Interpolate(
            new { dogPredicate },
            static (x, o) => o.Name == "Jon" && o.Dogs.Any(x.Splice(x.Data.dogPredicate))
        );

        var catPredicate = ExpressionOn<Cat>.Interpolate(
            new { ownerPredicate },
            static (x, c) => c.Name == "Garfield" && x.SpliceBody(c.Owner, x.Data.ownerPredicate)
        );

        var expected = ExpressionOn<Cat>.Of(
            c => c.Name == "Garfield"
            && (c.Owner.Name == "Jon" && c.Owner.Dogs.Any(d => d.Name == "Odie"))
        );

        Assert.Equivalent(expected, catPredicate);
    }

    [Fact]
    public void Splice_example_should_work() {
        var projection = ExpressionOn<string>.Of(v => v.Length);

        var interpolated0 = ExpressionOn<IEnumerable<string>>.Interpolate(
            new { projection },
            static (x, e) => e.Select(x.Splice(x.Data.projection))
        );

        Assert.Equivalent(
            expected: ExpressionOn<IEnumerable<string>>.Of(e => e.Select(v => v.Length)),
            actual: interpolated0
        );

        Assert.Equivalent(
            expected: ExpressionOnNone.Of(() => Math.Abs(2)),
            actual: ExpressionOnNone.Interpolate(x => Math.Abs(x.Splice<int>(Expression.Constant(2))))
        );
    }

    [Fact]
    public void SpliceValue_example_should_work() {
        var interpolated = ExpressionOnNone.Interpolate(x => x.SpliceValue(42));

        Assert.Equivalent(
            expected: ExpressionOnNone.Of(() => 42),
            actual: interpolated
        );
    }
}
