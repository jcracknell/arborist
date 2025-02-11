using Arborist.TestFixtures;

namespace Arborist;

public class ConvertTests {
    [Fact]
    public void As_should_work_as_expected() {
        var expected = ExpressionOn<Cat>.Of(c => c.Id as object);
        var actual = ExpressionOn<Cat>.As<object>(ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
    }

    [Fact]
    public void As_should_throw_for_invalid_conversions() {
        // N.B. that Expression.TypeAs throws an ArgumentException, not an InvalidOperationException
        // as seen in Convert/ConvertChecked.
        Assert.ThrowsAny<ArgumentException>(() => {
            ExpressionOn<Cat>.As<int>(ExpressionOn<Cat>.Of(c => c));
        });
    }

    [Fact]
    public void As_should_throw_for_invalid_expression_argument_types() {
        Assert.ThrowsAny<InvalidOperationException>(() => {
            ExpressionOn<Cat>.As<object>(ExpressionOn<Dog>.Of(d => d.Id));
        });
    }

    [Fact]
    public void Convert_should_work_as_expected() {
        var expected = ExpressionOn<Cat>.Of(c => (object)c.Id);
        var actual = ExpressionOn<Cat>.Convert<object>(ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.Convert, actual.Body.NodeType);
    }

    [Fact]
    public void Convert_should_throw_for_invalid_conversions() {
        Assert.ThrowsAny<InvalidOperationException>(() => {
            ExpressionOn<Cat>.Convert<int>(ExpressionOn<Cat>.Of(c => c));
        });
    }

    [Fact]
    public void Convert_should_throw_for_invalid_expression_argument_types() {
        Assert.ThrowsAny<InvalidOperationException>(() => {
            ExpressionOn<Cat>.Convert<object>(ExpressionOn<Dog>.Of(d => d.Id));
        });
    }

    [Fact]
    public void ConvertChecked_should_work_as_expected() {
        var expected = ExpressionOn<Cat>.Of(c => checked((short)c.Id));
        var actual = ExpressionOn<Cat>.ConvertChecked<short>(ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.ConvertChecked, actual.Body.NodeType);
    }

    [Fact]
    public void ConvertChecked_should_throw_for_invalid_conversions() {
        Assert.ThrowsAny<InvalidOperationException>(() => {
            ExpressionOn<Cat>.ConvertChecked<int>(ExpressionOn<Cat>.Of(c => c));
        });
    }

    [Fact]
    public void ConvertChecked_should_throw_for_invalid_expression_argument_types() {
        Assert.ThrowsAny<InvalidOperationException>(() => {
            ExpressionOn<Cat>.ConvertChecked<object>(ExpressionOn<Dog>.Of(d => d.Id));
        });
    }

    [Fact]
    public void ConvertChecked_should_fall_back_to_convert_when_no_checked_conversion_exists() {
        var expected = ExpressionOn<Cat>.Of(c => (object)c.Id);
        var actual = ExpressionOn<Cat>.ConvertChecked<object>(ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.Convert, actual.Body.NodeType);
    }
}
