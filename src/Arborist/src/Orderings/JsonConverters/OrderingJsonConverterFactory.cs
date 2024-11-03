using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arborist.Orderings.JsonConverters;

public class OrderingJsonConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && typeof(IOrderingLike).IsAssignableFrom(typeToConvert)
        && typeof(Ordering<>).Equals(typeToConvert.GetGenericTypeDefinition());

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var selectorType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OrderingJsonConverter<>).MakeGenericType(selectorType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
