using Arborist.TestFixtures;
using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void InterpolateRuntimeFallback_should_throw_InterpolatedParameterCaptureException() {
        var spliceBodyMethod = typeof(IInterpolationContext).GetMethods().Single(m => m.GetParameters().Length == 2);
        var parameters = spliceBodyMethod.GetParameters();

        Assert.True(parameters[0].IsDefined(typeof(InterpolatedSpliceParameterAttribute), false));
        Assert.True(parameters[1].IsDefined(typeof(EvaluatedSpliceParameterAttribute), false));

        Assert.Throws<InterpolatedParameterCaptureException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceBody(o, y => o));
        });
    }

    [Fact]
    public void InterpolateRuntimeFallback_should_throw_EvaluatedSpliceException() {
        Assert.Throws<InterpolationContextEvaluationException>(() => {
            ExpressionOnNone.InterpolateRuntimeFallback(default(object), x => x.SpliceValue(x.SpliceValue(1) + 2));
        });
    }

    [Fact]
    public void Interpolate_Splice_should_work_as_expected() {
        var data = new {
            Addition = Expression.Add(Expression.Constant(1), Expression.Constant(2))
        };
        
        var interpolated = InterpolationTestOnNone.Interpolate(data, x =>
            2 * x.Splice<int>(x.Data.Addition)
        );

        var expected = Expression.Lambda<Func<int>>(
            Expression.Multiply(Expression.Constant(2), data.Addition)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_as_expected_for_0_parameters() {
        var data = new {
            Spliced = ExpressionOnNone.Of(() => "foo")
        };
        
        var interpolated = InterpolationTestOnNone.Interpolate(data, x => x.SpliceBody(x.Data.Spliced).Length);

        var expected = Expression.Lambda<Func<int>>(
            body: Expression.Property(
                Expression.Constant("foo"),
                typeof(string).GetProperty(nameof(string.Length))!
            )
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_as_expected_for_1_parameter() {
        var data = new {
            OwnerName = ExpressionOn<Owner>.Of(o => o.Name)
        };
        
        var interpolated = InterpolationTestOn<Owner>.Interpolate(data, (x, o) =>
            x.SpliceBody(o, x.Data.OwnerName).Length
        );

        var expected = ExpressionOn<Owner>.Of(o => o.Name.Length);

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_within_a_lambda() {
        var data = new {
            CatName = ExpressionOn<Cat>.Of(c => c.Name)
        };

        var interpolated = InterpolationTestOn<Owner>.Interpolate(data, (x, o) =>
            o.CatsEnumerable.Any(c => x.SpliceBody(c, x.Data.CatName) == "Garfield")
        );

        var compiled = interpolated.Compile();

        Assert.True(compiled(new() { CatsEnumerable = [new() { Name  = "Garfield" }] }));
        Assert.False(compiled(new() { CatsEnumerable = [new() { Name  = "Nermal" }] }));
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_within_a_subexpression() {
        var data = new {
            CatName = ExpressionOn<Cat>.Of(c => c.Name)
        };
        
        var interpolated = InterpolationTestOn<Owner>.Interpolate(data, (x, o) =>
            o.CatsQueryable.Any(c => x.SpliceBody(c, x.Data.CatName) == "Garfield")
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            o.CatsQueryable.Any(c => c.Name == "Garfield")
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceQuoted_should_work_as_expected() {
        var data = new {
            Quoted = ExpressionOn<Cat>.Of(c => true)
        };

        var interpolated = InterpolationTestOn<Owner>.Interpolate(data, (x, o) =>
            o.CatsQueryable.Any(x.SpliceQuoted(x.Data.Quoted))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsQueryable.Any(c => true));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceValue_should_embed_constants() {
        var interpolated = InterpolationTestOnNone.Interpolate(default(object), x => x.SpliceValue("foo"));

        var expected = Expression.Lambda<Func<string>>(Expression.Constant("foo"));

        Assert.Equivalent(expected, interpolated);
    }
}
