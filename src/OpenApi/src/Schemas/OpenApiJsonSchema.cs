// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

[JsonConverter(typeof(JsonConverter))]
internal sealed partial class OpenApiJsonSchema(OpenApiSchema schema)
{
    /// <summary>
    /// Represents the OpenAPI schema that this instance represents.
    /// </summary>
    public OpenApiSchema Schema { get; } = schema;

    internal sealed class JsonConverter : JsonConverter<OpenApiJsonSchema>
    {
        public override OpenApiJsonSchema? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var schema = new OpenApiSchema();
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token to represent beginning of schema.");
            }
            reader.Read();
            do
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var propertyName = reader.GetString() ?? throw new JsonException("Encountered unexpected missing property name.");
                        ReadProperty(ref reader, propertyName, schema, options);
                        break;
                    case JsonTokenType.EndObject:
                        return new OpenApiJsonSchema(schema);
                    default:
                        continue;
                }
            } while (reader.Read());

            throw new JsonException("Encountered unexpected EOF token without producing a schema.");
        }

        /// <remarks>
        /// Intentionally not implemented. We don't expect to serialize OpenApiJsonSchema instances, only the underlying
        /// <see cref="Schema"/>.
        /// </remarks>
        public override void Write(Utf8JsonWriter writer, OpenApiJsonSchema value, JsonSerializerOptions options)
        {
            throw new NotSupportedException("OpenApiJsonSchema serialization is not supported.");
        }
    }
}
