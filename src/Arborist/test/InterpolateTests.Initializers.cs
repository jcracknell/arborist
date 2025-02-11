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
        Assert.Equal(ExpressionType.NewArrayInit, expected.Body.NodeType);
    }
    
    [Fact]
    public void Should_handle_array_initializer_with_explicit_dimensions() {
        var expected = ExpressionOnNone.Of(() => new string[2] { "foo", "bar" });
        
        var interpolated = InterpolationTestOnNone.Interpolate(
            default(object),
            x => new string[2] { x.SpliceValue("foo"), "bar" }
        );
        
        Assert.Equivalent(expected, interpolated);
        Assert.Equal(ExpressionType.NewArrayInit, expected.Body.NodeType);
    }
    
    [Fact]
    public void Should_handle_array_with_explicit_dimensions() {
        // N.B. you can't combine an initializer with non-constant array dimensions (in fact you cannot
        // combine an initializer with dimensions at all in an expression tree, as multidimensional
        // initializers are forbidden and a uni-dimensional initializer implies the length)
        var interpolated = InterpolationTestOnNone.Interpolate(
            default(object),
            x => new string[x.SpliceValue(3), 42]
        );
        
        var expected = ExpressionOnNone.Of(() => new string[3, 42]);
        
        Assert.Equivalent(expected, interpolated);
        Assert.Equal(ExpressionType.NewArrayBounds, expected.Body.NodeType);
    }
    
    [Fact]
    public void Should_handle_nested_array_with_explicit_dimensions() {
        var interpolated = InterpolationTestOnNone.Interpolate(
            default(object),
            x => new string[x.SpliceValue(3), 42][]
        );
        
        var expected = ExpressionOnNone.Of(() => new string[3, 42][]);
        
        Assert.Equivalent(expected, interpolated);
        Assert.Equal(ExpressionType.NewArrayBounds, expected.Body.NodeType);
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
