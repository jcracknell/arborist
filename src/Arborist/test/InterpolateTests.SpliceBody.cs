using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void SpliceBody_should_work_for_Func1_provided_via_data() {
        var interpolated = ExpressionOnNone.Interpolate(
            new { Thunk = ExpressionOnNone.Of(() => 41) },
            x => x.SpliceBody(x.Data.Thunk) + 1
        );

        var expected = Expression.Lambda<Func<int>>(
            Expression.Add(Expression.Constant(41), Expression.Constant(1))
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceBody_should_work_for_Func1_provided_as_literal() {
        var interpolated = ExpressionOnNone.Interpolate(
            default(object),
            x => x.SpliceBody(() => 41) + 1
        );

        var expected = Expression.Lambda<Func<int>>(
            Expression.Add(Expression.Constant(41), Expression.Constant(1))
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceBody_should_work_for_Func2_provided_via_data() {
        var interpolated = ExpressionOn<Cat>.Interpolate(
            new { OwnerPredicate = ExpressionOn<Owner>.Of(o => o.Name == "Jon") },
            (x, c) => x.SpliceBody(c.Owner, x.Data.OwnerPredicate)
        );

        var expected = ExpressionOn<Cat>.Of(c => c.Owner.Name == "Jon");

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceBody_should_work_for_Func2_provided_as_literal() {
        var interpolated = ExpressionOn<Cat>.Interpolate(
            default(object),
            (x, c) => x.SpliceBody(c.Owner, o => o.Name == "Jon")
        );

        var expected = ExpressionOn<Cat>.Of(c => c.Owner.Name == "Jon");

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceBody_should_handle_splices_in_interpolated_arguments() {
        // The order in which the splicing visitor handles the subtrees of the invocation matters, as
        // they are evaluated in order.
        var interpolated = ExpressionOnNone.Interpolate(
            new { HashCode = ExpressionOn<int>.Of(i => i.GetHashCode()) },
            x => x.SpliceBody(x.SpliceConstant(42), x.Data.HashCode)
        );

        var expected = ExpressionOnNone.Of(() => 42.GetHashCode());

        Assert.Equivalent(expected, interpolated);
    }
}
