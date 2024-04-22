// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

internal sealed partial class OpenApiJsonSchema
{
    /// <summary>
    /// Read a list from the given JSON reader instance.
    /// </summary>
    /// <typeparam name="T">The type of the elements that will populate the list.</typeparam>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to consume the list from.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> instance.</param>
    /// <returns>A list parsed from the JSON array.</returns>
    public static List<T>? ReadList<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
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
                values.Add(JsonSerializer.Deserialize<T>(ref reader, options)!);
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
    /// <param name="options">The <see cref="JsonSerializerOptions"/> instance.</param>
    /// <returns>A dictionary parsed from the JSON object.</returns>
    /// <exception cref="JsonException">Thrown if JSON object is not valid.</exception>
    public static Dictionary<string, T>? ReadDictionary<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
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
            values[key] = JsonSerializer.Deserialize<T>(ref reader, options)!;
            reader.Read();
        }

        return values;
    }

    internal static IOpenApiAny? ReadOpenApiAny(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new OpenApiNull();
        }

        if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
        {
            return new OpenApiBoolean(reader.GetBoolean());
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var intValue))
            {
                return new OpenApiInteger(intValue);
            }

            if (reader.TryGetInt64(out var longValue))
            {
                return new OpenApiLong(longValue);
            }

            if (reader.TryGetSingle(out var floatValue) && !float.IsInfinity(floatValue))
            {
                return new OpenApiFloat(floatValue);
            }

            if (reader.TryGetDouble(out var doubleValue))
            {
                return new OpenApiDouble(doubleValue);
            }
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return new OpenApiString(reader.GetString());
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var array = new OpenApiArray();
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                array.Add(ReadOpenApiAny(ref reader));
                reader.Read();
            }
            return array;
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var obj = new OpenApiObject();
            reader.Read();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName");
                }

                var key = reader.GetString()!;
                reader.Read();
                obj[key] = ReadOpenApiAny(ref reader);
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
                    var types = ReadList<string>(ref reader, options);
                    foreach (var type in types ?? [])
                    {
                        // JSON Schema represents nullable types using an array consisting of
                        // the target type and "null". Since OpenAPI Schema does not support
                        // representing types within an array we must check for the "null" type
                        // and map it to OpenAPI's `nullable` property for OpenAPI v3.
                        if (type == "null")
                        {
                            schema.Nullable = true;
                        }
                        else
                        {
                            schema.Type = type;
                        }
                    }
                }
                else
                {
                    var type = reader.GetString();
                    schema.Type = type;
                }
                break;
            case OpenApiSchemaKeywords.EnumKeyword:
                reader.Read();
                var enumValues = ReadList<string>(ref reader, options);
                if (enumValues is not null)
                {
                    schema.Enum = enumValues.Select(v => new OpenApiString(v)).ToList<IOpenApiAny>();
                }
                break;
            case OpenApiSchemaKeywords.DefaultKeyword:
                reader.Read();
                schema.Default = ReadOpenApiAny(ref reader);
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
                schema.Required = ReadList<string>(ref reader, options)?.ToHashSet();
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
                var props = ReadDictionary<OpenApiJsonSchema>(ref reader, options);
                schema.Properties = props?.ToDictionary(p => p.Key, p => p.Value.Schema);
                break;
            case OpenApiSchemaKeywords.AnyOfKeyword:
                reader.Read();
                schema.Type = "object";
                var schemas = ReadList<OpenApiJsonSchema>(ref reader, options);
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
                var mappings = ReadDictionary<string>(ref reader, options);
                schema.Discriminator.Mapping = mappings;
                break;
        }
    }
}
