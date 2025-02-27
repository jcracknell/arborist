namespace Arborist.Orderings;

public class OrderingTermTests {
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
