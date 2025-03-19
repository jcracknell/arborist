using Arborist.TestFixtures;
using Arborist.Utils;

namespace Arborist;

public class ConvertTests {
    [Fact]
    public void Convert_should_work_as_expected_for_Func1() {
        var expected = ExpressionOnNone.Of(() => (object)42);
        var actual = ExpressionHelper.Convert(TypeOf<object>.Value, ExpressionOnNone.Of(() => 42));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.Convert, actual.Body.NodeType);
        Assert.Equal(typeof(object), actual.ReturnType);
    }

    [Fact]
    public void Convert_should_work_as_expected_for_Func2() {
        var expected = ExpressionOn<Cat>.Of(c => (object)c.Id);
        var actual = ExpressionHelper.Convert(TypeOf<object>.Value, ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.Convert, actual.Body.NodeType);
        Assert.Equal(typeof(object), actual.ReturnType);
    }

    [Fact]
    public void Convert_should_work_as_expected_for_Func3() {
        var expected = ExpressionOn<Cat, Cat>.Of((c, d) => (object)c.Id);
        var actual = ExpressionHelper.Convert(TypeOf<object>.Value, ExpressionOn<Cat, Cat>.Of((c, d) => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.Convert, actual.Body.NodeType);
        Assert.Equal(typeof(object), actual.ReturnType);
    }

    [Fact]
    public void Convert_should_work_as_expected_for_Func4() {
        var expected = ExpressionOn<Cat, Cat, Cat>.Of((c, d, e) => (object)c.Id);
        var actual = ExpressionHelper.Convert(TypeOf<object>.Value, ExpressionOn<Cat, Cat, Cat>.Of((c, d, e) => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.Convert, actual.Body.NodeType);
        Assert.Equal(typeof(object), actual.ReturnType);
    }

    [Fact]
    public void Convert_should_work_as_expected_for_Func5() {
        var expected = ExpressionOn<Cat, Cat, Cat, Cat>.Of((c, d, e, f) => (object)c.Id);
        var actual = ExpressionHelper.Convert(TypeOf<object>.Value, ExpressionOn<Cat, Cat, Cat, Cat>.Of((c, d, e, f) => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.Convert, actual.Body.NodeType);
        Assert.Equal(typeof(object), actual.ReturnType);
    }

    [Fact]
    public void Convert_should_throw_for_invalid_conversions() {
        Assert.ThrowsAny<InvalidOperationException>(() => {
            ExpressionHelper.Convert(TypeOf<int>.Value, ExpressionOn<Cat>.Of(c => c));
        });
    }

    [Fact]
    public void ConvertChecked_should_work_as_expected_for_Func1() {
        var expected = ExpressionOnNone.Interpolate(x => checked((short)x.SpliceConstant(42)));
        var actual = ExpressionHelper.ConvertChecked(TypeOf<short>.Value, ExpressionOnNone.Of(() => 42));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.ConvertChecked, actual.Body.NodeType);
        Assert.Equal(typeof(short), actual.ReturnType);
    }

    [Fact]
    public void ConvertChecked_should_work_as_expected_for_Func2() {
        var expected = ExpressionOn<Cat>.Of(c => checked((short)c.Id));
        var actual = ExpressionHelper.ConvertChecked(TypeOf<short>.Value, ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.ConvertChecked, actual.Body.NodeType);
        Assert.Equal(typeof(short), actual.ReturnType);
    }

    [Fact]
    public void ConvertChecked_should_work_as_expected_for_Func3() {
        var expected = ExpressionOn<Cat, Cat>.Of((c, d) => checked((short)c.Id));
        var actual = ExpressionHelper.ConvertChecked(TypeOf<short>.Value, ExpressionOn<Cat, Cat>.Of((c, d) => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.ConvertChecked, actual.Body.NodeType);
        Assert.Equal(typeof(short), actual.ReturnType);
    }

    [Fact]
    public void ConvertChecked_should_work_as_expected_for_Func4() {
        var expected = ExpressionOn<Cat, Cat, Cat>.Of((c, d, e) => checked((short)c.Id));
        var actual = ExpressionHelper.ConvertChecked(TypeOf<short>.Value, ExpressionOn<Cat, Cat, Cat>.Of((c, d, e) => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.ConvertChecked, actual.Body.NodeType);
        Assert.Equal(typeof(short), actual.ReturnType);
    }

    [Fact]
    public void ConvertChecked_should_work_as_expected_for_Func5() {
        var expected = ExpressionOn<Cat, Cat, Cat, Cat>.Of((c, d, e, f) => checked((short)c.Id));
        var actual = ExpressionHelper.ConvertChecked(TypeOf<short>.Value, ExpressionOn<Cat, Cat, Cat, Cat>.Of((c, d, e, f) => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.ConvertChecked, actual.Body.NodeType);
        Assert.Equal(typeof(short), actual.ReturnType);
    }

    [Fact]
    public void ConvertChecked_should_throw_for_invalid_conversions() {
        Assert.ThrowsAny<InvalidOperationException>(() => {
            ExpressionHelper.ConvertChecked(TypeOf<int>.Value, ExpressionOn<Cat>.Of(c => c));
        });
    }

    [Fact]
    public void ConvertChecked_should_fall_back_to_convert_when_no_checked_conversion_exists() {
        var expected = ExpressionOn<Cat>.Of(c => (object)c.Id);
        var actual = ExpressionHelper.ConvertChecked(TypeOf<object>.Value, ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.Convert, actual.Body.NodeType);
        Assert.Equal(typeof(object), actual.ReturnType);
    }
}
