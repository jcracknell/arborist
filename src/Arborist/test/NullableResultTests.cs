using Arborist.TestFixtures;

namespace Arborist;

public class NullableResultTests {
    [Fact]
    public void NullableResult0_should_work_as_expected_for_reference() {
        var expected = ExpressionOnNone.Of(() => "foo");
        var actual = ExpressionHelper.NullableResult(ExpressionOnNone.Of(() => "foo"));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void NullableResult0_should_work_as_expected_for_struct() {
        var expected = ExpressionOnNone.Of(() => (Nullable<int>)42);
        var actual = ExpressionHelper.NullableResult(ExpressionOnNone.Of(() => 42));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void NullableResult1_should_work_as_expected_for_reference() {
        var expected = (Expression<Func<Cat, string?>>)(c => c.Name);
        var actual = ExpressionHelper.NullableResult(ExpressionOn<Cat>.Of(c => c.Name));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void NullableResult1_should_work_as_expected_for_struct() {
        var expected = ExpressionOn<Cat>.Of(c => (Nullable<int>)c.Id);
        var actual = ExpressionHelper.NullableResult(ExpressionOn<Cat>.Of(c => c.Id));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void NullableResult2_should_work_as_expected_for_reference() {
        var expected = (Expression<Func<Cat, Owner, string?>>)((c, d) => c.Name);
        var actual = ExpressionHelper.NullableResult(ExpressionOn<Cat, Owner>.Of((c, d) => c.Name));

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void NullableResult2_should_work_as_expected_for_struct() {
        var expected = ExpressionOn<Cat, Owner>.Of((c, d) => (Nullable<int>)c.Id);
        var actual = ExpressionHelper.NullableResult(ExpressionOn<Cat, Owner>.Of((c, d) => c.Id));

        Assert.Equivalent(expected, actual);
    }
}
