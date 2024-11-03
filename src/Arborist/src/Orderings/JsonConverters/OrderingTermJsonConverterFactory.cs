using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arborist.Orderings.JsonConverters;

public class OrderingTermJsonConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && typeof(IOrderingTermLike).IsAssignableFrom(typeToConvert)
        && typeof(OrderingTerm<>) == typeToConvert.GetGenericTypeDefinition();

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var selectorType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OrderingTermJsonConverter<>).MakeGenericType(selectorType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
