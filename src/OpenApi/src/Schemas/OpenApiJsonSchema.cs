// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;

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
            var schema = new OpenApiJsonSchemaModel();
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token to represent beginning of schema.");
            }

            OpenApiJsonSchemaContext? context = null;
            reader.Read();
            do
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var propertyName = reader.GetString() ?? throw new JsonException("Encountered unexpected missing property name.");
                        context ??= (options.TypeInfoResolver as OpenApiJsonSchemaContext) ?? new OpenApiJsonSchemaContext(new(options));
                        ReadProperty(ref reader, propertyName, schema, options, context);
                        break;
                    case JsonTokenType.EndObject:
                        if (schema.Metadata?.ContainsKey(Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaTuplePrefixItems) == true)
                        {
                            return new OpenApiJsonSchema(schema);
                        }

                        return new OpenApiJsonSchema((OpenApiSchema)schema.CreateShallowCopy());
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

    internal sealed class OpenApiJsonSchemaModel : OpenApiSchema
    {
        public override void SerializeAsV31(IOpenApiWriter writer)
            => SerializeWithStandardKeywords(writer, static (schema, target) => schema.SerializeAsV31Core(target));

        public override void SerializeAsV32(IOpenApiWriter writer)
            => SerializeWithStandardKeywords(writer, static (schema, target) => schema.SerializeAsV32Core(target));

        private void SerializeAsV31Core(IOpenApiWriter writer)
            => base.SerializeAsV31(writer);

        private void SerializeAsV32Core(IOpenApiWriter writer)
            => base.SerializeAsV32(writer);

        private void SerializeWithStandardKeywords(
            IOpenApiWriter writer,
            Action<OpenApiJsonSchemaModel, IOpenApiWriter> serialize)
        {
            if (UnrecognizedKeywords is null ||
                !UnrecognizedKeywords.ContainsKey(OpenApiSchemaKeywords.PrefixItemsKeyword))
            {
                serialize(this, writer);
                return;
            }

            using var textWriter = new StringWriter(CultureInfo.InvariantCulture);
            var intermediateWriter = new OpenApiJsonWriter(textWriter);
            serialize(this, intermediateWriter);
            var serializedSchema = JsonNode.Parse(textWriter.ToString())!.AsObject();
            var unrecognizedKeywords = serializedSchema[Microsoft.OpenApi.OpenApiConstants.UnrecognizedKeywords]!.AsObject();
            foreach (var (keyword, value) in unrecognizedKeywords)
            {
                serializedSchema[keyword] = value?.DeepClone();
            }
            serializedSchema.Remove(Microsoft.OpenApi.OpenApiConstants.UnrecognizedKeywords);
            writer.WriteAny(serializedSchema);
        }
    }
}
