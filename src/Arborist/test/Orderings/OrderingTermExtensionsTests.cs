namespace Arborist.Orderings;

public class OrderingTermExtensionsTests {
    [Fact]
    public void InvertDirection_should_work_as_expected() {
        Assert.Equal(OrderingDirection.Descending, OrderingDirection.Ascending.InvertDirection());
        Assert.Equal(OrderingDirection.Ascending, OrderingDirection.Descending.InvertDirection());
    }
}
