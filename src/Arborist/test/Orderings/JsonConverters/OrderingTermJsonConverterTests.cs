using System.Text.Json;

namespace Arborist.Orderings.JsonConverters;

public class OrderingTermJsonConverterTests {
    [Fact]
    public void Should_serialize_as_expected() {
        var actual = JsonSerializer.Serialize(OrderingTerm.Create("foo", OrderingDirection.Ascending));
        Assert.Equal("[\"foo\",\"asc\"]", actual);
    }

    [Fact]
    public void Should_deserialize_as_expected() {
        var actual = JsonSerializer.Deserialize<OrderingTerm<string>>("[\"foo\",\"asc\"]");
        Assert.Equal(OrderingTerm.Create("foo", OrderingDirection.Ascending), actual);
    }
}
