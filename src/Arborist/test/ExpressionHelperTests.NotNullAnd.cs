namespace Arborist;

public partial class ExpressionHelperTests {
    [Fact]
    public void NotNullAnd_should_work_as_expected_for_reference_types() {
        var expected = ExpressionOn<string?>.Of(s => s != null && s.Length == 0);
        var actual = ExpressionHelper.NotNullAnd((string s) => s.Length == 0);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void NotNullAnd_should_work_as_expected_for_value_types() {
        var expected = ExpressionOn<int?>.Of(i => i.HasValue && i.Value % 2 == 0);
        var actual = ExpressionHelper.NotNullAnd((int i) => i % 2 == 0);

        Assert.Equivalent(expected, actual);
    }
}
