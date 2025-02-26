using System.Text.Json;

namespace Arborist.Orderings.JsonConverters;

public class OrderingDirectionJsonConverterTests {
    [Fact]
    public void Should_serialize_Ascending_as_expected() {
        var actual = JsonSerializer.Serialize(OrderingDirection.Ascending);
        Assert.Equal("\"asc\"", actual);
    }

    [Fact]
    public void Should_serialize_Descending_as_expected() {
        var actual = JsonSerializer.Serialize(OrderingDirection.Descending);
        Assert.Equal("\"desc\"", actual);
    }

    [Fact]
    public void Should_deserialize_from_Ascending_prefix() {
        var actual = JsonSerializer.Deserialize<OrderingDirection>("\"a\"");
        Assert.Equal(OrderingDirection.Ascending, actual);
    }

    [Fact]
    public void Should_deserialize_from_Descending_prefix() {
        var actual = JsonSerializer.Deserialize<OrderingDirection>("\"d\"");
        Assert.Equal(OrderingDirection.Descending, actual);
    }

    [Fact]
    public void Should_throw_deserializing_invalid_prefix() {
        Assert.ThrowsAny<JsonException>(() => {
            JsonSerializer.Deserialize<OrderingDirection>("\"x\"");
        });
    }
}
