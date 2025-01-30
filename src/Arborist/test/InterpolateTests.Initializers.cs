using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_work_for_object_initializer() {
        Assert.Equivalent(
            expected: ExpressionOnNone.Of(() => new Cat { Id = 42, Name = "Garfield" }),
            #pragma warning disable ARB003
            actual: ExpressionOnNone.Interpolate(default(object), x => new Cat { Id = 42, Name = "Garfield" })
            #pragma warning restore
        );
    }
    
    [Fact]
    public void Should_work_for_collection_initializer() {
        Assert.Equivalent(
            expected: ExpressionOnNone.Of(() => new List<Cat> { new Cat() }),
            #pragma warning disable ARB003
            actual: ExpressionOnNone.Interpolate(default(object), x => new List<Cat> { new Cat() })
            #pragma warning restore
        );
    }
}
