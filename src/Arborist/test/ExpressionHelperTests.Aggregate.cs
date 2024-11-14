using Arborist.Fixtures;

namespace Arborist;

public partial class ExpressionHelperTests {
    [Fact]
    public void Aggregate0_should_work_as_expected() {
        var expected = ExpressionOnNone.Of(() => (Math.Abs(0) + Math.Abs(1)) + Math.Abs(2));
        var actual = ExpressionHelper.Aggregate(
            expressions: new[] {
                ExpressionOnNone.Of(() => Math.Abs(1)),
                ExpressionOnNone.Of(() => Math.Abs(2))
            },
            seed: ExpressionOnNone.Of(() => Math.Abs(0)),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Aggregate1_should_work_as_expected() {
        var expected = ExpressionOn<Cat>.Of(c => "" + c.Name + c.Owner!.Name);
        var actual = ExpressionHelper.Aggregate(
            expressions: new[] {
                ExpressionOn<Cat>.Of(c => c.Name),
                ExpressionOn<Cat>.Of(c => c.Owner!.Name)
            },
            seed: ExpressionOn<Cat>.Of(c => ""),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Aggregate2_should_work_as_expected() {
        var expected = ExpressionOn<Cat, Owner>.Of((c, o) => "" + c.Name + o.Name);
        var actual = ExpressionHelper.Aggregate(
            expressions: new[] {
                ExpressionOn<Cat, Owner>.Of((c, o) => c.Name),
                ExpressionOn<Cat, Owner>.Of((c, o) => o.Name)
            },
            seed: ExpressionOn<Cat, Owner>.Of((p0, p1) => ""),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Aggregate3_should_work_as_expected() {
        var expected = ExpressionOn<Cat, Owner, int>.Of((c, o, i) => "" + c.Name + o.Name + i.ToString());
        var actual = ExpressionHelper.Aggregate(
            expressions: new[] {
                ExpressionOn<Cat, Owner, int>.Of((c, o, i) => c.Name),
                ExpressionOn<Cat, Owner, int>.Of((c, o, i) => o.Name),
                ExpressionOn<Cat, Owner, int>.Of((c, o, i) => i.ToString())
            },
            seed: ExpressionOn<Cat, Owner, int>.Of((p0, p1, p2) => ""),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Aggregate4_should_work_as_expected() {
        var expected = ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => "" + c.Name + o.Name + i.ToString() + s.ToLowerInvariant());
        var actual = ExpressionHelper.Aggregate(
            expressions: new[] {
                ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => c.Name),
                ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => o.Name),
                ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => i.ToString()),
                ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => s.ToLowerInvariant())
            },
            seed: ExpressionOn<Cat, Owner, int, string>.Of((p0, p1, p2, p3) => ""),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }
}
