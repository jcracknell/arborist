using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arborist.Orderings.JsonConverters;

public class OrderingTermJsonConverter<TSelector> : JsonConverter<OrderingTerm<TSelector>> {
    public override OrderingTerm<TSelector>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if(JsonTokenType.Null == reader.TokenType)
            return null;

        if(JsonTokenType.StartArray != reader.TokenType)
            throw new JsonException();
        if(!reader.Read())
            throw new JsonException();

        var selector = JsonSerializer.Deserialize<TSelector>(ref reader, options);

        if(!reader.Read())
            throw new JsonException();

        var direction = JsonSerializer.Deserialize<OrderingDirection>(ref reader, options);

        if(!reader.Read())
            throw new JsonException();

        if(JsonTokenType.EndArray != reader.TokenType)
            throw new JsonException();

        return OrderingTerm.Create(selector, direction)!;
    }

    public override void Write(Utf8JsonWriter writer, OrderingTerm<TSelector> value, JsonSerializerOptions options) {
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value.Selector, options);
        JsonSerializer.Serialize(writer, value.Direction, options);
        writer.WriteEndArray();
    }
}
