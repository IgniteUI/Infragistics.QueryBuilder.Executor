using System;
using System.IO;
using System.Text.Json.Nodes; // for JsonValue
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Infragistics.QueryBuilder.Executor
{
    public static class QueryExecutorExtensions
    {
        public static IServiceCollection AddQueryBuilder<TMyDbContext, TResults>(this IServiceCollection services)
            where TResults : class
        {
            services.AddScoped<QueryBuilderService<TMyDbContext, TResults>>();
            return services;
        }

        public static IEndpointRouteBuilder UseQueryBuilder<TMyDbContext, TResults>(
            this IEndpointRouteBuilder endpoints, string path)
            where TMyDbContext : class
            where TResults : class
        {
            endpoints.MapPost(path, async (HttpContext ctx, QueryBuilderService<TMyDbContext, TResults> svc) =>
            {
                string json;
                using (var sr = new StreamReader(ctx.Request.Body))
                {
                    json = await sr.ReadToEndAsync();
                }

                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    DateParseHandling = Newtonsoft.Json.DateParseHandling.None,
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                };

                // 1) Convert QueryFilter by shape (tree vs leaf)
                settings.Converters.Add(new QueryFilterCreationConverter());
                // 2) Let Newtonsoft write primitives (number/string/bool) into System.Text.Json.Nodes.JsonValue
                settings.Converters.Add(new NewtonsoftJsonValueConverter());

                var query = Newtonsoft.Json.JsonConvert.DeserializeObject<Query>(json, settings);
                if (query is null)
                    return Results.BadRequest("Wrong or missing query");

                var result = svc.RunQuery(query);
                return Results.Ok(result);
            })
            .WithTags(["QueryBuilder"])
            .Accepts<Query>("application/json")
            .Produces<TResults>();

            return endpoints;
        }
    }

    /// <summary>
    /// Resolves QueryFilter to the only concrete types available:
    /// - FilteringExpressionsTree (has "filteringOperands")
    /// - FilteringExpression      (leaf)
    /// </summary>
    file sealed class QueryFilterCreationConverter : Newtonsoft.Json.JsonConverter
    {
        private static readonly Type BaseType = typeof(QueryFilter);
        private static readonly Type TreeType = typeof(FilteringExpressionsTree);
        private static readonly Type LeafType = typeof(FilteringExpression);

        public override bool CanConvert(Type objectType) => objectType == BaseType;

        public override object ReadJson(
            Newtonsoft.Json.JsonReader reader,
            Type objectType,
            object? existingValue,
            Newtonsoft.Json.JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            // Tree if it has an array "filteringOperands"; otherwise leaf
            var isTree = jo["filteringOperands"] is JArray;
            var target = isTree ? TreeType : LeafType;

            return jo.ToObject(target, serializer)!;
        }

        public override void WriteJson(
            Newtonsoft.Json.JsonWriter writer,
            object? value,
            Newtonsoft.Json.JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>
    /// Newtonsoft -> System.Text.Json.Nodes.JsonValue bridge.
    /// Handles primitives and null (which is all your payload needs for "searchVal": 10253).
    /// </summary>
    file sealed class NewtonsoftJsonValueConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(JsonValue);

        public override object? ReadJson(
            Newtonsoft.Json.JsonReader reader,
            Type objectType,
            object? existingValue,
            Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == Newtonsoft.Json.JsonToken.Null)
                return null;

            // Extract the raw .NET value from Newtonsoft token
            var token = JToken.ReadFrom(reader);

            // For numbers/bools/strings, token.ToObject<object>() is a primitive -> wrap it
            // For safety, we only support primitives here since JsonValue is a primitive wrapper.
            // (Your payload uses numbers/strings/null for searchVal.)
            var primitive = token.Type switch
            {
                JTokenType.Integer => (object)token.ToObject<long>()!,
                JTokenType.Float => token.ToObject<double>()!,
                JTokenType.Boolean => token.ToObject<bool>()!,
                JTokenType.String => token.ToObject<string>()!,
                JTokenType.Null => null!,
                _ => throw new NotSupportedException(
                        $"JsonValue converter only supports primitives. Got {token.Type}.")
            };

            return primitive is null ? null : JsonValue.Create(primitive);
        }

        public override void WriteJson(
            Newtonsoft.Json.JsonWriter writer,
            object? value,
            Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            var jv = (JsonValue)value;
            // Extract the underlying primitive and write it as a native JSON value
            if (jv.TryGetValue(out long l)) { writer.WriteValue(l); return; }
            if (jv.TryGetValue(out double d)) { writer.WriteValue(d); return; }
            if (jv.TryGetValue(out bool b)) { writer.WriteValue(b); return; }
            if (jv.TryGetValue(out string s)) { writer.WriteValue(s); return; }

            // Fallback: write as string representation
            writer.WriteValue(jv.ToJsonString());
        }
    }
}