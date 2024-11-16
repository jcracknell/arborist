using Arborist.Fixtures;

namespace Arborist;

public partial class ExpressionHelperTests {
    [Fact]
    public void AggregateTree0_should_work_as_expected() {
        var expected = ExpressionOnNone.Of(() => Math.Abs(1) + Math.Abs(2));
        var actual = ExpressionHelper.AggregateTree(
            expressions: new[] {
                ExpressionOnNone.Of(() => Math.Abs(1)),
                ExpressionOnNone.Of(() => Math.Abs(2))
            },
            fallback: ExpressionOnNone.Of(() => Math.Abs(0)),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void AggregateTree1_should_work_as_expected() {
        var expected = ExpressionOn<Cat>.Of(c => c.Name + c.Owner!.Name);
        var actual = ExpressionHelper.AggregateTree(
            expressions: new[] {
                ExpressionOn<Cat>.Of(c => c.Name),
                ExpressionOn<Cat>.Of(c => c.Owner!.Name)
            },
            fallback: ExpressionOn<Cat>.Of(c => ""),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void AggregateTree2_should_work_as_expected() {
        var expected = ExpressionOn<Cat, Owner>.Of((c, o) => c.Name + o.Name);
        var actual = ExpressionHelper.AggregateTree(
            expressions: new[] {
                ExpressionOn<Cat, Owner>.Of((c, o) => c.Name),
                ExpressionOn<Cat, Owner>.Of((c, o) => o.Name)
            },
            fallback: ExpressionOn<Cat, Owner>.Of((c, o) => ""),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void AggregateTree3_should_work_as_expected() {
        var expected = ExpressionOn<Cat, Owner, int>.Of((c, o, i) => (c.Name + o.Name) + i.ToString());
        var actual = ExpressionHelper.AggregateTree(
            expressions: new[] {
                ExpressionOn<Cat, Owner, int>.Of((c, o, i) => c.Name),
                ExpressionOn<Cat, Owner, int>.Of((c, o, i) => o.Name),
                ExpressionOn<Cat, Owner, int>.Of((c, o, i) => i.ToString())
            },
            fallback: ExpressionOn<Cat, Owner, int>.Of((c, o, i) => ""),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void AggregateTree4_should_work_as_expected() {
        var expected = ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => (c.Name + o.Name) + (i.ToString() + s.ToLowerInvariant()));
        var actual = ExpressionHelper.AggregateTree(
            expressions: new[] {
                ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => c.Name),
                ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => o.Name),
                ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => i.ToString()),
                ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => s.ToLowerInvariant())
            },
            fallback: ExpressionOn<Cat, Owner, int, string>.Of((c, o, i, s) => ""),
            binaryOperator: (acc, v) => acc + v
        );

        Assert.Equivalent(expected, actual);
    }
}
