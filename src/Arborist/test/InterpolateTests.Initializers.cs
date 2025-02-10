using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_array_initializer() {
        var interpolated = InterpolationTestOnNone.Interpolate(default(object),
            x => new[] { x.SpliceValue("foo"), "bar", x.SpliceValue("baz") }
        );
        
        var expected = ExpressionOnNone.Of(() => new[] { "foo", "bar", "baz" });
        
        Assert.Equivalent(expected, interpolated);
    }
    
    [Fact]
    public void Should_work_for_object_initializer() {
        var interpolated = InterpolationTestOnNone.Interpolate(default(object), x =>
            new Cat { Id = x.SpliceValue(42), Name = x.SpliceValue("Garfield") }
        );
        
        var expected = ExpressionOnNone.Of(() => new Cat { Id = 42, Name = "Garfield" });
    
        Assert.Equivalent(expected, interpolated);
    }
    
    [Fact]
    public void Should_work_for_collection_initializer() {
        var interpolated = InterpolationTestOnNone.Interpolate(default(object), x =>
            new List<int> { x.SpliceValue(42) }
        );
        
        var expected = ExpressionOnNone.Of(() => new List<int> { 42 });
        
        Assert.Equivalent(expected, interpolated);
    }
    
    [Fact]
    public void Should_work_for_object_initializer_in_collection_initializer() {
        var interpolated = InterpolationTestOnNone.Interpolate(default(object), x =>
            new List<Cat> { new Cat { Name = x.SpliceValue("Garfield") } }
        );
        
        var expected = ExpressionOnNone.Of(() => new List<Cat> { new Cat { Name = "Garfield" } });
        
        Assert.Equivalent(expected, interpolated);
    }
}
