using Arborist.TestFixtures;

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
        var interpolated0 = ExpressionOn<IEnumerable<string>>.Interpolate(
            new { Projection = ExpressionOn<string>.Of(v => v.Length) },
            static (x, e) => e.Select(x.Splice(x.Data.Projection))
        );

        Assert.Equivalent(
            expected: ExpressionOn<IEnumerable<string>>.Of(e => e.Select(v => v.Length)),
            actual: interpolated0
        );

        var interpolated1 = ExpressionOnNone.Interpolate(
            new { Expr = Expression.Constant(42) },
            static x => Math.Abs(x.Splice<int>(x.Data.Expr))
        );

        Assert.Equivalent(
            expected: ExpressionOnNone.Of(() => Math.Abs(42)),
            actual: interpolated1
        );
    }

    [Fact]
    public void SpliceBody_example_should_work() {
        var interpolated = ExpressionOn<Cat>.Interpolate(
            new { Predicate = ExpressionOn<Owner>.Of(o => o.Name == "Jon") },
            static (x, c) => x.SpliceBody(c.Owner, x.Data.Predicate)
        );

        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => c.Owner.Name == "Jon"),
            actual: interpolated
        );
    }

    [Fact]
    public void SpliceQuoted_example_should_work() {
        var interpolated = ExpressionOn<IQueryable<Cat>>.Interpolate(
            new { Predicate = ExpressionOn<Cat>.Of(c => c.Age == 8) },
            static (x, q) => q.Any(x.SpliceQuoted(x.Data.Predicate))
        );

        Assert.Equivalent(
            expected: ExpressionOn<IQueryable<Cat>>.Of(q => q.Any(c => c.Age == 8)),
            actual: interpolated
        );
    }

    [Fact]
    public void SpliceValue_example_should_work() {
        var interpolated = ExpressionOnNone.Interpolate(
            new { Value = 42 },
            static x => x.SpliceValue(x.Data.Value)
        );

        Assert.Equivalent(
            expected: ExpressionOnNone.Of(() => 42),
            actual: interpolated
        );
    }

    [Fact]
    public void And_example_should_work() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(
                c => (((c.Name == "Garfield") && c.Name == "Nermal") && c.Name == "Arlene") && c.Name == "Mom"
            ),
            actual: ExpressionHelper.And([
                ExpressionOn<Cat>.Of(c => c.Name == "Garfield"),
                ExpressionOn<Cat>.Of(c => c.Name == "Nermal"),
                ExpressionOn<Cat>.Of(c => c.Name == "Arlene"),
                ExpressionOn<Cat>.Of(c => c.Name == "Mom")
            ])
        );
    }

    [Fact]
    public void AndTree_example_should_work() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(
                c => (c.Name == "Garfield" && c.Name == "Nermal") && (c.Name == "Arlene" && c.Name == "Mom")
            ),
            actual: ExpressionHelper.AndTree([
                ExpressionOn<Cat>.Of(c => c.Name == "Garfield"),
                ExpressionOn<Cat>.Of(c => c.Name == "Nermal"),
                ExpressionOn<Cat>.Of(c => c.Name == "Arlene"),
                ExpressionOn<Cat>.Of(c => c.Name == "Mom")
            ])
        );
    }

    [Fact]
    public void Not_example_should_work() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(c => !(c.Name == "Garfield")),
            actual: ExpressionHelper.Not(ExpressionOn<Cat>.Of(c => c.Name == "Garfield"))
        );
    }

    [Fact]
    public void NotNullAnd_should_work() {
        Assert.Equivalent(
            expected: ExpressionOn<string?>.Of(s => s != null && s.Length == 4),
            actual: ExpressionHelper.NotNullAnd(ExpressionOn<string>.Of(s => s.Length == 4))
        );
        Assert.Equivalent(
            expected: ExpressionOn<int?>.Of(i => i.HasValue && i.Value % 2 == 0),
            actual: ExpressionHelper.NotNullAnd(ExpressionOn<int>.Of(i => i % 2 == 0))
        );
    }

    [Fact]
    public void NullOr_should_work() {
        Assert.Equivalent(
            expected: ExpressionOn<string?>.Of(s => s == null || s.Length == 4),
            actual: ExpressionHelper.NullOr(ExpressionOn<string>.Of(s => s.Length == 4))
        );
        Assert.Equivalent(
            expected: ExpressionOn<int?>.Of(i => !i.HasValue || i.Value % 2 == 0),
            actual: ExpressionHelper.NullOr(ExpressionOn<int>.Of(i => i % 2 == 0))
        );
    }

    [Fact]
    public void Or_example_should_work() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(
                c => (((c.Name == "Garfield") || c.Name == "Nermal") || c.Name == "Arlene") || c.Name == "Mom"
            ),
            actual: ExpressionHelper.Or([
                ExpressionOn<Cat>.Of(c => c.Name == "Garfield"),
                ExpressionOn<Cat>.Of(c => c.Name == "Nermal"),
                ExpressionOn<Cat>.Of(c => c.Name == "Arlene"),
                ExpressionOn<Cat>.Of(c => c.Name == "Mom")
            ])
        );
    }

    [Fact]
    public void OrTree_example_should_work() {
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of(
                c => (c.Name == "Garfield" || c.Name == "Nermal") || (c.Name == "Arlene" || c.Name == "Mom")
            ),
            actual: ExpressionHelper.OrTree([
                ExpressionOn<Cat>.Of(c => c.Name == "Garfield"),
                ExpressionOn<Cat>.Of(c => c.Name == "Nermal"),
                ExpressionOn<Cat>.Of(c => c.Name == "Arlene"),
                ExpressionOn<Cat>.Of(c => c.Name == "Mom")
            ])
        );
    }
}
