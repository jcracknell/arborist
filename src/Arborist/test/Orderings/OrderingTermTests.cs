namespace Arborist.Orderings;

public class OrderingTermTests {
    [Fact]
    public void Ascending_should_work_as_expected() {
        var actual = OrderingTerm.Ascending("foo");

        Assert.Equal("foo", actual.Selector);
        Assert.Equal(OrderingDirection.Ascending, actual.Direction);
    }

    [Fact]
    public void Descending_should_work_as_expected() {
        var actual = OrderingTerm.Descending("foo");

        Assert.Equal("foo", actual.Selector);
        Assert.Equal(OrderingDirection.Descending, actual.Direction);
    }

    [Fact]
    public void ApplyDirection_should_work_as_expected() {
        Assert.Equal(
            expected: OrderingTerm.Ascending("foo"),
            actual: OrderingTerm.Ascending("foo").ApplyDirection(OrderingDirection.Ascending)
        );
        Assert.Equal(
            expected: OrderingTerm.Descending("foo"),
            actual: OrderingTerm.Ascending("foo").ApplyDirection(OrderingDirection.Descending)
        );
        Assert.Equal(
            expected: OrderingTerm.Descending("foo"),
            actual: OrderingTerm.Descending("foo").ApplyDirection(OrderingDirection.Ascending)
        );
        Assert.Equal(
            expected: OrderingTerm.Ascending("foo"),
            actual: OrderingTerm.Descending("foo").ApplyDirection(OrderingDirection.Descending)
        );
    }
}
