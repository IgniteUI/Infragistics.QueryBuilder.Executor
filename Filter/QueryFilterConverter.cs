using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infragistics.QueryBuilder.Executor.Filter
{
    internal class QueryFilterConverter : System.Text.Json.Serialization.JsonConverter<QueryFilter>
    {
        public override QueryFilter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return System.Text.Json.JsonSerializer.Deserialize<QueryFilter>(doc.RootElement.GetRawText(), options)!;
        }

        public override void Write(Utf8JsonWriter writer, QueryFilter value, JsonSerializerOptions options)
            => System.Text.Json.JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}