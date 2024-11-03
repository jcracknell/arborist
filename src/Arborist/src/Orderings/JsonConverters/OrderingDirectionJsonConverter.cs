using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arborist.Orderings.JsonConverters;

public class OrderingDirectionJsonConverter : JsonConverter<OrderingDirection> {
    public override OrderingDirection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if(reader.GetString() is { Length: not 0 } str) {
            if("ascending".StartsWith(str, StringComparison.OrdinalIgnoreCase))
                return OrderingDirection.Ascending;
            if("descending".StartsWith(str, StringComparison.OrdinalIgnoreCase))
                return OrderingDirection.Descending;
        }

        throw new JsonException($"Expected {nameof(OrderingTerm)} value.");
    }

    public override void Write(Utf8JsonWriter writer, OrderingDirection value, JsonSerializerOptions options) {
        switch(value) {
            case OrderingDirection.Ascending:
                writer.WriteStringValue("asc");
                break;
            case OrderingDirection.Descending:
                writer.WriteStringValue("desc");
                break;
            default:
                throw new Exception($"Unhandled {nameof(OrderingDirection)} value: {value}.");
        }
    }
}
