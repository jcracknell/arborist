using Arborist.TestFixtures;

namespace Arborist;

public class NullableTests {
    [Fact]
    public void Nullable0_should_work_as_expected_for_reference() {
        var expected = ExpressionOnNone.Of(() => "foo");
        var actual = ExpressionOnNone.Nullable(() => "foo");

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Nullable0_should_work_as_expected_for_struct() {
        var expected = ExpressionOnNone.Of(() => (Nullable<int>)42);
        var actual = ExpressionOnNone.Nullable(() => 42);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Nullable1_should_work_as_expected_for_reference() {
        var expected = (Expression<Func<Cat, string?>>)(c => c.Name);
        var actual = ExpressionOn<Cat>.Nullable(c => c.Name);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Nullable1_should_work_as_expected_for_struct() {
        var expected = ExpressionOn<Cat>.Of(c => (Nullable<int>)c.Id);
        var actual = ExpressionOn<Cat>.Nullable(c => c.Id);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Nullable2_should_work_as_expected_for_reference() {
        var expected = (Expression<Func<Cat, Owner, string?>>)((c, d) => c.Name);
        var actual = ExpressionOn<Cat, Owner>.Nullable((c, d) => c.Name);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Nullable2_should_work_as_expected_for_struct() {
        var expected = ExpressionOn<Cat, Owner>.Of((c, d) => (Nullable<int>)c.Id);
        var actual = ExpressionOn<Cat, Owner>.Nullable((c, d) => c.Id);

        Assert.Equivalent(expected, actual);
    }
}
