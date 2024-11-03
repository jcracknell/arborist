using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arborist.Orderings.JsonConverters;

public class OrderingJsonConverter<TSelector> : JsonConverter<Ordering<TSelector>> {
    public override Ordering<TSelector>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if(JsonTokenType.Null == reader.TokenType)
            return default;

        if(JsonTokenType.StartArray != reader.TokenType)
            throw new JsonException();
        if(!reader.Read())
            throw new JsonException();

        var builder = new OrderingBuilder<TSelector>();
        while(JsonTokenType.EndArray != reader.TokenType) {
            var term = JsonSerializer.Deserialize<OrderingTerm<TSelector>>(ref reader, options);
            if(term is null)
                throw new JsonException($"Encountered null {typeof(OrderingTerm<TSelector>)} reading {typeof(Ordering<TSelector>)}.");

            builder.Add(term);

            if(!reader.Read())
                throw new JsonException();
        }

        return builder.Build();
    }

    public override void Write(Utf8JsonWriter writer, Ordering<TSelector> value, JsonSerializerOptions options) {
        writer.WriteStartArray();

        var rest = value;
        while(!rest.IsEmpty) {
            JsonSerializer.Serialize(writer, rest.Term, options);
            rest = rest.Rest;
        }

        writer.WriteEndArray();
    }
}
