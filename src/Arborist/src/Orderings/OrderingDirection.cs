using Arborist.Orderings.JsonConverters;
using System.Text.Json.Serialization;

namespace Arborist.Orderings;

[JsonConverter(typeof(OrderingDirectionJsonConverter))]
public enum OrderingDirection {
    Ascending,
    Descending
}
