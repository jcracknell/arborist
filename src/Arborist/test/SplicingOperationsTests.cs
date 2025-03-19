using Arborist.Interpolation;
using Arborist.TestFixtures;

namespace Arborist;

public class SplicingOperationsTests {
    [Fact]
    public void Splice_expression_should_throw_InvalidOperationException_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.Splice<int>(
                default(IInterpolationContext)!,
                default(Expression)!
            );
        });
    }

    [Fact]
    public void Splice_lambda_should_throw_InvalidOperationException_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.Splice(
                default(IInterpolationContext)!,
                default(Expression<Func<Cat, string>>)!
            );
        });
    }

    [Fact]
    public void SpliceBody0_should_throw_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.SpliceBody(
                default(IInterpolationContext)!,
                default(Expression<Func<string>>)!
            );
        });
    }

    [Fact]
    public void SpliceBody1_should_throw_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.SpliceBody(
                default(IInterpolationContext)!,
                default(Cat)!,
                default(Expression<Func<Cat, string>>)!
            );
        });
    }

    [Fact]
    public void SpliceBody2_should_throw_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.SpliceBody(
                default(IInterpolationContext)!,
                default(Cat)!,
                default(Owner)!,
                default(Expression<Func<Cat, Owner, string>>)!
            );
        });
    }

    [Fact]
    public void SpliceBody3_should_throw_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.SpliceBody(
                default(IInterpolationContext)!,
                default(Cat)!,
                default(Owner)!,
                default(Dog),
                default(Expression<Func<Cat, Owner, Dog, string>>)!
            );
        });
    }

    [Fact]
    public void SpliceBody4_should_throw_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.SpliceBody(
                default(IInterpolationContext)!,
                default(Cat)!,
                default(Owner)!,
                default(Dog),
                default(int),
                default(Expression<Func<Cat, Owner, Dog, int, string>>)!
            );
        });
    }

    [Fact]
    public void SpliceConstant_should_throw_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.SpliceConstant(
                default(IInterpolationContext)!,
                42
            );
        });
    }

    [Fact]
    public void SpliceQuoted_should_throw_InvalidOperationException_when_invoked() {
        Assert.Throws<InvalidOperationException>(() => {
            SplicingOperations.SpliceQuoted(default(IInterpolationContext)!, default(Expression<Func<Cat, string>>)!);
        });
    }
}
