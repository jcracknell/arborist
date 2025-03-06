using Arborist.Interpolation;
using Arborist.TestFixtures;

namespace Arborist;

public class InterpolateRuntimeFallbackTests {
    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException() {
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceConstant(o.Name));
        });
    }

    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException_for_evaluated_context_reference() {
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceConstant(x));
        });
    }

    [Fact]
    public void Should_not_throw_InterpolatedParameterEvaluationException_for_spliced_data() {
        ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceConstant(x.Data));
    }

    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException_for_evaluated_splice() {
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceConstant(x.SpliceConstant(32)));
        });
    }

    [Fact]
    public void Should_throw_InterpolationContextReferenceException() {
        Assert.Throws<InterpolationContextReferenceException>(() => {
            ExpressionOnNone.InterpolateRuntimeFallback(default(object), x => x.Data == x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void Should_not_throw_InterpolationContextReferenceException_for_shadowed_reference() {
        ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => o.Cats.Single(x => x.Id == 1));
    }

    [Fact]
    public void Splice_should_work_as_expected() {
        var interpolated = ExpressionOn<Owner>.InterpolateRuntimeFallback(
            new { Predicate = ExpressionOn<Cat>.Of(c => c.Age == 8) },
            (x, o) => o.CatsEnumerable.Any(x.Splice(x.Data.Predicate))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsEnumerable.Any(c => c.Age == 8));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceBody_should_work_as_expected() {
        var interpolated = ExpressionOn<Cat>.InterpolateRuntimeFallback(
            new { OwnerPredicate = ExpressionOn<Owner>.Of(o => o.Name == "Jon") },
            (x, c) => x.SpliceBody(c.Owner, x.Data.OwnerPredicate)
        );

        var expected = ExpressionOn<Cat>.Of(c => c.Owner.Name == "Jon");

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceBody_should_work_as_expected_for_thunk() {
        var interpolated = ExpressionOnNone.InterpolateRuntimeFallback(
            new { Thunk = ExpressionOnNone.Of(() => 41) },
            x => x.SpliceBody(x.Data.Thunk) + 1
        );

        var expected = Expression.Lambda<Func<int>>(
            Expression.Add(Expression.Constant(41), Expression.Constant(1))
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceQuoted_should_work_as_expected() {
        var data = new {
            Quoted = ExpressionOn<Cat>.Of(c => true)
        };

        var interpolated = ExpressionOn<Owner>.InterpolateRuntimeFallback(data, (x, o) =>
            o.CatsQueryable.Any(x.SpliceQuoted(x.Data.Quoted))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsQueryable.Any(c => true));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void SpliceConstant_should_work_as_expected() {
        var interpolated = ExpressionOnNone.InterpolateRuntimeFallback(
            default(object),
            x => x.SpliceConstant("foo")
        );

        var expected = ExpressionOnNone.Of(() => "foo");

        Assert.Equivalent(expected, interpolated);
    }
}
