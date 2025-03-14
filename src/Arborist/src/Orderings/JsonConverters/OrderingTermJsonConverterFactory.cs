using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arborist.Orderings.JsonConverters;

public class OrderingTermJsonConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) {
        if(!typeToConvert.IsGenericType)
            return false;
        if(!typeof(IOrderingTermLike).IsAssignableFrom(typeToConvert))
            return false;
        if(typeof(OrderingTerm<>).Equals(typeToConvert.GetGenericTypeDefinition()))
            return true;

        // Handle the case where we are serializing and the provided type is the runtime type
        foreach(var implemented in typeToConvert.GetInterfaces())
            if(implemented.IsGenericType && typeof(OrderingTerm<>).Equals(implemented.GetGenericTypeDefinition()))
                return true;

        return false;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var selectorType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OrderingTermJsonConverter<>).MakeGenericType(selectorType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
