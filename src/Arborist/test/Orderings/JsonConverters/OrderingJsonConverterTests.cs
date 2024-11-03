using System.Text.Json;

namespace Arborist.Orderings.JsonConverters;

public class OrderingJsonConverterTests {
    [Fact]
    public void Unordered_should_serialize_as_expected() {
        var actual = JsonSerializer.Serialize(Ordering<string>.Unordered);
        Assert.Equal("[]", actual);
    }

    [Fact]
    public void Unordered_should_deserialize_as_expected() {
        var actual = JsonSerializer.Deserialize<Ordering<string>>("[]");
        Assert.Equal(Ordering<string>.Unordered, actual);
    }

    [Fact]
    public void Should_serialize_1_term_as_expected() {
        var actual = JsonSerializer.Serialize(Ordering.ByAscending("foo"));
        Assert.Equal("[[\"foo\",\"asc\"]]", actual);
    }

    [Fact]
    public void Should_deserialize_1_term_as_expected() {
        var actual = JsonSerializer.Deserialize<Ordering<string>>("[[\"foo\",\"asc\"]]");
        Assert.Equal(Ordering.ByAscending("foo"), actual);
    }

    [Fact]
    public void Should_serialize_2_terms_as_expected() {
        var actual = JsonSerializer.Serialize(Ordering.ByAscending("foo").ThenByDescending("bar"));
        Assert.Equal("[[\"foo\",\"asc\"],[\"bar\",\"desc\"]]", actual);
    }

    [Fact]
    public void Should_deserialize_2_terms_as_expected() {
        var actual = JsonSerializer.Deserialize<Ordering<string>>("[[\"foo\",\"asc\"],[\"bar\",\"desc\"]]");
        Assert.Equal(Ordering.ByAscending("foo").ThenByDescending("bar"), actual);
    }
}
