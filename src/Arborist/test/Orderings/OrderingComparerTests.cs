namespace Arborist.Orderings;

public class OrderingComparerTests {
    [Fact]
    public void Should_work_as_expected_equal() {
        var comparer = new OrderingComparer<string>(Ordering.ByAscending(StringComparer.Ordinal));

        Assert.True(comparer.Compare("a", "a") == 0);
        Assert.False(comparer.Compare("a", "A") == 0);
    }

    [Fact]
    public void Should_work_as_expected_ascending() {
        var comparer = new OrderingComparer<string>(Ordering.ByAscending(StringComparer.Ordinal));

        Assert.True(comparer.Compare("a", "b") < 0);
    }

    [Fact]
    public void Should_work_as_expected_descending() {
        var comparer = new OrderingComparer<string>(Ordering.ByDescending(StringComparer.Ordinal));

        Assert.True(comparer.Compare("a", "b") > 0);
    }
}
