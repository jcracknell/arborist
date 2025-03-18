using Arborist.TestFixtures;

namespace Arborist;

public class NullConditionalTests {
    [Fact]
    public void Should_work_with_reference_input_and_reference_result() {
        var expected = ExpressionOn<Cat?>.Of(c => c != null ? c.Name : null);
        var actual = ExpressionHelper.NullConditional(ExpressionOn<Cat>.Of(c => c.Name));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Should_work_with_reference_input_and_struct_result() {
        var expected = ExpressionOn<Cat?>.Of(c => c != null ? (Nullable<int>)c.Id : null);
        var actual = ExpressionHelper.NullConditional(ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Should_work_with_reference_input_and_nullable_result() {
        var expected = ExpressionOn<Cat?>.Of(c => c != null ? c.Age : null);
        var actual = ExpressionHelper.NullConditional(ExpressionOn<Cat>.Of(c => c.Age));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Should_work_with_struct_input_and_reference_result() {
        var expected = ExpressionOn<int?>.Of(v => v != null ? v.Value.ToString() : null);
        var actual = ExpressionHelper.NullConditional(ExpressionOn<int>.Of(v => v.ToString()));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Should_work_with_struct_input_and_struct_result() {
        var expected = ExpressionOn<int?>.Of(v => v != null ? (Nullable<int>)v.Value : null);
        var actual = ExpressionHelper.NullConditional(ExpressionOn<int>.Of(v => v));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Should_work_with_struct_input_and_nullable_result() {
        var expected = ExpressionOn<int?>.Of(v => v != null ? new int?(v.Value) : null);
        var actual = ExpressionHelper.NullConditional(ExpressionOn<int>.Of(v => new int?(v)));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Should_avoid_unnecessary_cast() {
        var expected = ExpressionOn<Cat?>.Of<object?>(c => c != null ? c.Name : null);
        var actual = ExpressionHelper.NullConditional((Expression<Func<Cat, object>>)(c => c.Name));

        Assert.Equivalent(expected, actual);
    }
}
