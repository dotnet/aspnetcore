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
                        if (schema.Metadata?.ContainsKey(Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaPrefixItems) == true)
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
        public OpenApiJsonSchemaModel()
        {
        }

        public OpenApiJsonSchemaModel(OpenApiSchema schema)
        {
            Title = schema.Title;
            Schema = schema.Schema;
            Id = schema.Id;
            Comment = schema.Comment;
            Vocabulary = schema.Vocabulary;
            DynamicRef = schema.DynamicRef;
            DynamicAnchor = schema.DynamicAnchor;
            Definitions = schema.Definitions;
            Anchor = schema.Anchor;
            Type = schema.Type;
            Const = schema.Const;
            Format = schema.Format;
            Description = schema.Description;
            Maximum = schema.Maximum;
            Minimum = schema.Minimum;
            ExclusiveMaximum = schema.ExclusiveMaximum;
            ExclusiveMinimum = schema.ExclusiveMinimum;
            MaxLength = schema.MaxLength;
            MinLength = schema.MinLength;
            Pattern = schema.Pattern;
            MultipleOf = schema.MultipleOf;
            Default = schema.Default;
            ReadOnly = schema.ReadOnly;
            WriteOnly = schema.WriteOnly;
            AllOf = schema.AllOf;
            OneOf = schema.OneOf;
            AnyOf = schema.AnyOf;
            Not = schema.Not;
            Required = schema.Required;
            Items = schema.Items;
            MaxItems = schema.MaxItems;
            MinItems = schema.MinItems;
            UniqueItems = schema.UniqueItems;
            Contains = schema.Contains;
            MaxContains = schema.MaxContains;
            MinContains = schema.MinContains;
            Properties = schema.Properties;
            PatternProperties = schema.PatternProperties;
            MaxProperties = schema.MaxProperties;
            MinProperties = schema.MinProperties;
            AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed;
            AdditionalProperties = schema.AdditionalProperties;
            Discriminator = schema.Discriminator;
            Examples = schema.Examples;
            UnevaluatedProperties = schema.UnevaluatedProperties;
            UnevaluatedPropertiesSchema = schema.UnevaluatedPropertiesSchema;
            ContentEncoding = schema.ContentEncoding;
            ContentMediaType = schema.ContentMediaType;
            ContentSchema = schema.ContentSchema;
            PropertyNames = schema.PropertyNames;
            DependentSchemas = schema.DependentSchemas;
            DependentRequired = schema.DependentRequired;
            If = schema.If;
            Then = schema.Then;
            Else = schema.Else;
            Deprecated = schema.Deprecated;
            Extensions = schema.Extensions;
            UnrecognizedKeywords = schema.UnrecognizedKeywords;
            Metadata = schema.Metadata;
            if (schema.Enum is { Count: > 0 })
            {
                Enum = schema.Enum;
            }
        }

        public override void SerializeAsV31(IOpenApiWriter writer)
            => SerializeWithStandardKeywords(writer, isV31: true, static (schema, target) => schema.SerializeAsV31Core(target));

        public override void SerializeAsV32(IOpenApiWriter writer)
            => SerializeWithStandardKeywords(writer, isV31: false, static (schema, target) => schema.SerializeAsV32Core(target));

        private void SerializeAsV31Core(IOpenApiWriter writer)
            => base.SerializeAsV31(writer);

        private void SerializeAsV32Core(IOpenApiWriter writer)
            => base.SerializeAsV32(writer);

        private void SerializeWithStandardKeywords(
            IOpenApiWriter writer,
            bool isV31,
            Action<OpenApiJsonSchemaModel, IOpenApiWriter> serialize)
        {
            if (UnrecognizedKeywords is null &&
                Metadata?.ContainsKey(Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaPrefixItems) != true)
            {
                serialize(this, writer);
                return;
            }

            using var textWriter = new StringWriter(CultureInfo.InvariantCulture);
            var intermediateWriter = new OpenApiJsonWriter(textWriter);
            serialize(this, intermediateWriter);
            var serializedSchema = JsonNode.Parse(textWriter.ToString())!.AsObject();
            if (serializedSchema[Microsoft.OpenApi.OpenApiConstants.UnrecognizedKeywords] is JsonObject unrecognizedKeywords)
            {
                foreach (var (keyword, value) in unrecognizedKeywords)
                {
                    serializedSchema[keyword] = value?.DeepClone();
                }
                serializedSchema.Remove(Microsoft.OpenApi.OpenApiConstants.UnrecognizedKeywords);
            }

            if (Metadata?.TryGetValue(Microsoft.AspNetCore.OpenApi.OpenApiConstants.SchemaPrefixItems, out var prefixItemsValue) == true &&
                prefixItemsValue is IOpenApiSchema[] prefixItems)
            {
                var serializedPrefixItems = new JsonArray();
                foreach (var prefixItem in prefixItems)
                {
                    using var prefixTextWriter = new StringWriter(CultureInfo.InvariantCulture);
                    var prefixWriter = new OpenApiJsonWriter(prefixTextWriter);
                    if (isV31)
                    {
                        prefixItem.SerializeAsV31(prefixWriter);
                    }
                    else
                    {
                        prefixItem.SerializeAsV32(prefixWriter);
                    }
                    serializedPrefixItems.Add(JsonNode.Parse(prefixTextWriter.ToString()));
                }
                serializedSchema[OpenApiSchemaKeywords.PrefixItemsKeyword] = serializedPrefixItems;
            }
            writer.WriteAny(serializedSchema);
        }
    }
}
