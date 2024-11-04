namespace Arborist.Orderings;

public class OrderingTermExtensionsTests {
    [Fact]
    public void Reversed_should_work_as_expected() {
        Assert.Equal(OrderingDirection.Descending, OrderingDirection.Ascending.Reversed());
        Assert.Equal(OrderingDirection.Ascending, OrderingDirection.Descending.Reversed());
    }
}
