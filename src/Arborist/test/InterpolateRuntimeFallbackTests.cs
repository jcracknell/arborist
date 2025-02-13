using Arborist.Interpolation;
using Arborist.TestFixtures;

namespace Arborist;

public class InterpolateRuntimeFallbackTests {
    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException() {
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceValue(o.Name));
        });
    }

    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException_for_evaluated_context_reference() {
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceValue(x));
        });
    }

    [Fact]
    public void Should_not_throw_InterpolatedParameterEvaluationException_for_spliced_data() {
        ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceValue(x.Data));
    }

    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException_for_evaluated_splice() {
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceValue(x.SpliceValue(32)));
        });
    }

    [Fact]
    public void Should_throw_InterpolationContextReferenceException() {
        Assert.Throws<InterpolationContextReferenceException>(() => {
            ExpressionOnNone.InterpolateRuntimeFallback(default(object), x => x.Data == x.SpliceValue(x.Data));
        });
    }

    [Fact]
    public void Should_not_throw_InterpolationContextReferenceException_for_shadowed_reference() {
        ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => o.Cats.Single(x => x.Id == 1));
    }
}
