namespace Arborist;

public partial class ExpressionHelperTests {
    [Fact]
    public void NullOr_should_work_as_expected_for_reference_types() {
        var expected = ExpressionHelper.On<string?>().Of(s => s == null || s.Length == 0);
        var actual = ExpressionHelper.NullOr((string s) => s.Length == 0);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void NullOr_should_work_as_expected_for_value_types() {
        var expected = ExpressionHelper.On<int?>().Of(i => !i.HasValue || i.Value % 2 == 0);
        var actual = ExpressionHelper.NullOr((int i) => i % 2 == 0);

        Assert.Equivalent(expected, actual);
    }
}