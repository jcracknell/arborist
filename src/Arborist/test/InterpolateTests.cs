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
}
