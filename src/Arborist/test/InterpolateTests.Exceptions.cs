using Arborist.Interpolation;
using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException() {
        #pragma warning disable ARB003
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceConstant(o.Name));
        });
        #pragma warning restore
    }

    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException_for_evaluated_context_reference() {
        #pragma warning disable ARB002
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceConstant(x));
        });
        #pragma warning restore
    }

    [Fact]
    public void Should_not_throw_InterpolatedParameterEvaluationException_for_spliced_data() {
        ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceConstant(x.Data));
    }

    [Fact]
    public void Should_throw_InterpolatedParameterEvaluationException_for_evaluated_splice() {
        #pragma warning disable ARB002
        Assert.Throws<InterpolatedParameterEvaluationException>(() => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => x.SpliceConstant(x.SpliceConstant(32)));
        });
        #pragma warning restore
    }

    [Fact]
    public void Should_throw_InterpolationContextReferenceException() {
        #pragma warning disable ARB002
        Assert.Throws<InterpolationContextReferenceException>(() => {
            ExpressionOnNone.InterpolateRuntimeFallback(default(object), x => x.Data == x.SpliceConstant(x.Data));
        });
        #pragma warning restore
    }

    [Fact]
    public void Should_not_throw_InterpolationContextReferenceException_for_shadowed_reference() {
        #pragma warning disable ARB001
        ExpressionOn<Owner>.InterpolateRuntimeFallback(default(object), (x, o) => o.Cats.Single(x => x.Id == 1));
        #pragma warning restore
    }

    [Fact]
    public void Should_throw_SpliceArgumentEvaluationException_wrapping_inner_exception() {
        Func<int> notImplemented = () => throw new NotImplementedException();

        var thrown = Assert.Throws<SpliceArgumentEvaluationException>(() => {
            return ExpressionOn<Owner>.InterpolateRuntimeFallback((x, o) => x.SpliceConstant(notImplemented()));
        });

        Assert.IsType<NotImplementedException>(thrown.InnerException);
    }
}
