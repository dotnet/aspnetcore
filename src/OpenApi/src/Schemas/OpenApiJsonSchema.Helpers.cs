// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using OpenApiConstants = Microsoft.AspNetCore.OpenApi.OpenApiConstants;

internal sealed partial class OpenApiJsonSchema
{
    /// <summary>
    /// Read a list from the given JSON reader instance.
    /// </summary>
    /// <typeparam name="T">The type of the elements that will populate the list.</typeparam>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to consume the list from.</param>
    /// <returns>A list parsed from the JSON array.</returns>
    public static List<T>? ReadList<T>(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var values = new List<T>();
            reader.Read();
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                values.Add((T)JsonSerializer.Deserialize(ref reader, typeof(T), OpenApiJsonSchemaContext.Default)!);
                reader.Read();
            }

            return values;
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        return default;
    }

    /// <summary>
    /// Read a dictionary from the given JSON reader instance.
    /// </summary>
    /// <typeparam name="T">The type associated with the values in the dictionary.</typeparam>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to consume the dictionary from.</param>
    /// <returns>A dictionary parsed from the JSON object.</returns>
    /// <exception cref="JsonException">Thrown if JSON object is not valid.</exception>
    public static Dictionary<string, T>? ReadDictionary<T>(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject or Null");
        }

        var values = new Dictionary<string, T>();
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName");
            }

            var key = reader.GetString()!;
            reader.Read();
            values[key] = (T)JsonSerializer.Deserialize(ref reader, typeof(T), OpenApiJsonSchemaContext.Default)!;
            reader.Read();
        }

        return values;
    }

    internal static JsonNode? ReadJsonNode(ref Utf8JsonReader reader)
        => ReadJsonNode(ref reader, out _);

    internal static JsonNode? ReadJsonNode(ref Utf8JsonReader reader, out JsonSchemaType? type)
    {
        type = null;
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
        {
            type = JsonSchemaType.Boolean;
            return reader.GetBoolean();
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var intValue))
            {
                type = JsonSchemaType.Integer;
                return intValue;
            }

            if (reader.TryGetInt64(out var longValue))
            {
                type = JsonSchemaType.Integer;
                return longValue;
            }

            if (reader.TryGetSingle(out var floatValue) && !float.IsInfinity(floatValue))
            {
                type = JsonSchemaType.Number;
                return floatValue;
            }

            if (reader.TryGetDouble(out var doubleValue))
            {
                type = JsonSchemaType.Number;
                return doubleValue;
            }
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            type = JsonSchemaType.String;
            return reader.GetString();
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            type = JsonSchemaType.Array;
            var array = new JsonArray();
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                array.Add(ReadJsonNode(ref reader));
                reader.Read();
            }
            return array;
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            type = JsonSchemaType.Object;
            var obj = new JsonObject();
            reader.Read();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName");
                }

                var key = reader.GetString()!;
                reader.Read();
                obj[key] = ReadJsonNode(ref reader);
                reader.Read();
            }
            return obj;
        }

        return default;
    }

    /// <summary>
    /// Read a property node from the given JSON reader instance.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to consume the property value from.</param>
    /// <param name="propertyName">The name of the property the editor is currently consuming.</param>
    /// <param name="schema">The <see cref="OpenApiSchema"/> to write the given values to.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> instance.</param>
    public static void ReadProperty(ref Utf8JsonReader reader, string propertyName, OpenApiSchema schema, JsonSerializerOptions options)
    {
        switch (propertyName)
        {
            case OpenApiSchemaKeywords.TypeKeyword:
                reader.Read();
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var types = ReadList<string>(ref reader);
                    foreach (var type in types ?? [])
                    {
                        if (schema.Type is not null)
                        {
                            schema.Type |= Enum.Parse<JsonSchemaType>(type, ignoreCase: true);
                        }
                        else
                        {
                            schema.Type = Enum.Parse<JsonSchemaType>(type, ignoreCase: true);
                        }
                    }
                }
                else
                {
                    var type = reader.GetString();
                    Debug.Assert(type is not null);
                    schema.Type = Enum.Parse<JsonSchemaType>(type, ignoreCase: true);
                }
                break;
            case OpenApiSchemaKeywords.EnumKeyword:
                reader.Read();
                var enumValues = ReadList<JsonNode>(ref reader);
                if (enumValues is not null)
                {
                    schema.Enum = enumValues;
                }
                break;
            case OpenApiSchemaKeywords.DefaultKeyword:
                reader.Read();
                schema.Default = ReadJsonNode(ref reader);
                break;
            case OpenApiSchemaKeywords.ItemsKeyword:
                reader.Read();
                var valueConverter = (JsonConverter<OpenApiJsonSchema>)options.GetTypeInfo(typeof(OpenApiJsonSchema)).Converter;
                schema.Items = valueConverter.Read(ref reader, typeof(OpenApiJsonSchema), options)?.Schema;
                break;
            case OpenApiSchemaKeywords.NullableKeyword:
                reader.Read();
                schema.Nullable = reader.GetBoolean();
                break;
            case OpenApiSchemaKeywords.DescriptionKeyword:
                reader.Read();
                schema.Description = reader.GetString();
                break;
            case OpenApiSchemaKeywords.FormatKeyword:
                reader.Read();
                schema.Format = reader.GetString();
                break;
            case OpenApiSchemaKeywords.RequiredKeyword:
                reader.Read();
                schema.Required = ReadList<string>(ref reader)?.ToHashSet();
                break;
            case OpenApiSchemaKeywords.MinLengthKeyword:
                reader.Read();
                var minLength = reader.GetInt32();
                schema.MinLength = minLength;
                break;
            case OpenApiSchemaKeywords.MinItemsKeyword:
                reader.Read();
                var minItems = reader.GetInt32();
                schema.MinItems = minItems;
                break;
            case OpenApiSchemaKeywords.MaxLengthKeyword:
                reader.Read();
                var maxLength = reader.GetInt32();
                schema.MaxLength = maxLength;
                break;
            case OpenApiSchemaKeywords.MaxItemsKeyword:
                reader.Read();
                var maxItems = reader.GetInt32();
                schema.MaxItems = maxItems;
                break;
            case OpenApiSchemaKeywords.MinimumKeyword:
                reader.Read();
                var minimum = reader.GetDecimal();
                schema.Minimum = minimum;
                break;
            case OpenApiSchemaKeywords.MaximumKeyword:
                reader.Read();
                var maximum = reader.GetDecimal();
                schema.Maximum = maximum;
                break;
            case OpenApiSchemaKeywords.PatternKeyword:
                reader.Read();
                var pattern = reader.GetString();
                schema.Pattern = pattern;
                break;
            case OpenApiSchemaKeywords.PropertiesKeyword:
                reader.Read();
                var props = ReadDictionary<OpenApiJsonSchema>(ref reader);
                schema.Properties = props?.ToDictionary(p => p.Key, p => p.Value.Schema);
                break;
            case OpenApiSchemaKeywords.AdditionalPropertiesKeyword:
                reader.Read();
                if (reader.TokenType == JsonTokenType.False)
                {
                    schema.AdditionalPropertiesAllowed = false;
                    break;
                }
                var additionalPropsConverter = (JsonConverter<OpenApiJsonSchema>)options.GetTypeInfo(typeof(OpenApiJsonSchema)).Converter;
                schema.AdditionalProperties = additionalPropsConverter.Read(ref reader, typeof(OpenApiJsonSchema), options)?.Schema;
                break;
            case OpenApiSchemaKeywords.AnyOfKeyword:
                reader.Read();
                schema.Type = JsonSchemaType.Object;
                var schemas = ReadList<OpenApiJsonSchema>(ref reader);
                schema.AnyOf = schemas?.Select(s => s.Schema).ToList();
                break;
            case OpenApiSchemaKeywords.DiscriminatorKeyword:
                reader.Read();
                var discriminator = reader.GetString();
                if (discriminator is not null)
                {
                    schema.Discriminator = new OpenApiDiscriminator { PropertyName = discriminator };
                }
                break;
            case OpenApiSchemaKeywords.DiscriminatorMappingKeyword:
                reader.Read();
                var mappings = ReadDictionary<string>(ref reader);
                if (mappings is not null)
                {
                    schema.Discriminator.Mapping = mappings;
                }
                break;
            case OpenApiConstants.SchemaId:
                reader.Read();
                schema.Annotations ??= new Dictionary<string, object>();
                schema.Annotations.Add(OpenApiConstants.SchemaId, reader.GetString());
                break;
            // OpenAPI does not support the `const` keyword in its schema implementation, so
            // we map it to its closest approximation, an enum with a single value, here.
            case OpenApiSchemaKeywords.ConstKeyword:
                reader.Read();
                schema.Enum = [ReadJsonNode(ref reader, out var constType)];
                schema.Type = constType;
                break;
            case OpenApiSchemaKeywords.RefKeyword:
                reader.Read();
                schema.Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = reader.GetString() };
                break;
            default:
                reader.Skip();
                break;
        }
    }
}
