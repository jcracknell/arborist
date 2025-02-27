namespace Arborist.Orderings;

public class OrderingDirectionExtensionsTests {
    [Fact]
    public void ApplyDirection_should_work_as_expected() {
        Assert.Equal(
            expected: OrderingDirection.Ascending,
            actual: OrderingDirection.Ascending.ApplyDirection(OrderingDirection.Ascending)
        );
        Assert.Equal(
            expected: OrderingDirection.Descending,
            actual: OrderingDirection.Ascending.ApplyDirection(OrderingDirection.Descending)
        );
        Assert.Equal(
            expected: OrderingDirection.Descending,
            actual: OrderingDirection.Descending.ApplyDirection(OrderingDirection.Ascending)
        );
        Assert.Equal(
            expected: OrderingDirection.Ascending,
            actual: OrderingDirection.Descending.ApplyDirection(OrderingDirection.Descending)
        );
    }

    [Fact]
    public void Invert_should_work_as_expected() {
        Assert.Equal(OrderingDirection.Descending, OrderingDirection.Ascending.Invert());
        Assert.Equal(OrderingDirection.Ascending, OrderingDirection.Descending.Invert());
    }
}
