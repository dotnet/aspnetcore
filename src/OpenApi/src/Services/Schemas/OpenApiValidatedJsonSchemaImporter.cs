// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040 // The framework implements this experimental contract.

using System.Text.Json;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiValidatedJsonSchemaImporter
{
    public static OpenApiSchema Import(
        OpenApiValidatedJsonSchemaEvidence evidence,
        OpenApiSpecVersion openApiVersion)
    {
        if (openApiVersion == OpenApiSpecVersion.OpenApi3_0 &&
            (evidence.NormalizedSchema.TryGetProperty("$defs", out _) ||
             ContainsReference(evidence.NormalizedSchema)))
        {
            return CreateRoot(evidence.Identity);
        }

        var options = new JsonSerializerOptions();
        options.Converters.Add(new OpenApiSchemaJsonConverter(OpenApiSpecVersion.OpenApi3_1));
        var imported = ParseSchema(evidence.NormalizedUtf8Schema.Span, options);
        var result = ProcessSchema(imported, evidence.NormalizedSchema, options, openApiVersion);
        result.Metadata ??= new Dictionary<string, object>();
        result.Metadata[OpenApiConstants.SchemaValidatedIdentity] = evidence.Identity;
        return result;
    }

    private static OpenApiJsonSchema.OpenApiJsonSchemaModel ProcessSchema(
        OpenApiSchema schema,
        JsonElement source,
        JsonSerializerOptions options,
        OpenApiSpecVersion openApiVersion)
    {
        var result = new OpenApiJsonSchema.OpenApiJsonSchemaModel(schema);
        result.UnrecognizedKeywords?.Remove(OpenApiSchemaKeywords.PrefixItemsKeyword);

        result.Definitions = ProcessDictionary(result.Definitions, source, "$defs", options, openApiVersion);
        result.Properties = ProcessDictionary(result.Properties, source, "properties", options, openApiVersion);
        result.PatternProperties = ProcessDictionary(result.PatternProperties, source, "patternProperties", options, openApiVersion);
        result.DependentSchemas = ProcessDictionary(result.DependentSchemas, source, "dependentSchemas", options, openApiVersion);
        result.AllOf = ProcessList(result.AllOf, source, "allOf", options, openApiVersion);
        result.AnyOf = ProcessList(result.AnyOf, source, "anyOf", options, openApiVersion);
        result.OneOf = ProcessList(result.OneOf, source, "oneOf", options, openApiVersion);
        result.Not = ProcessChild(result.Not, source, "not", options, openApiVersion);
        result.Items = ProcessChild(result.Items, source, "items", options, openApiVersion);
        result.Contains = ProcessChild(result.Contains, source, "contains", options, openApiVersion);
        result.AdditionalProperties = ProcessChild(result.AdditionalProperties, source, "additionalProperties", options, openApiVersion);
        result.PropertyNames = ProcessChild(result.PropertyNames, source, "propertyNames", options, openApiVersion);
        result.UnevaluatedPropertiesSchema = ProcessChild(result.UnevaluatedPropertiesSchema, source, "unevaluatedProperties", options, openApiVersion);
        result.ContentSchema = ProcessChild(result.ContentSchema, source, "contentSchema", options, openApiVersion);
        result.If = ProcessChild(result.If, source, "if", options, openApiVersion);
        result.Then = ProcessChild(result.Then, source, "then", options, openApiVersion);
        result.Else = ProcessChild(result.Else, source, "else", options, openApiVersion);

        if (source.ValueKind == JsonValueKind.Object &&
            source.TryGetProperty(OpenApiSchemaKeywords.PrefixItemsKeyword, out var prefixItemsElement) &&
            prefixItemsElement.ValueKind == JsonValueKind.Array)
        {
            if (openApiVersion == OpenApiSpecVersion.OpenApi3_0)
            {
                result.Items = new OpenApiSchema();
            }
            else
            {
                var prefixItems = new List<IOpenApiSchema>();
                foreach (var element in prefixItemsElement.EnumerateArray())
                {
                    var prefixItem = ParseSchema(element, options);
                    prefixItems.Add(ProcessSchema(prefixItem, element, options, openApiVersion));
                }

                result.Metadata ??= new Dictionary<string, object>();
                result.Metadata[OpenApiConstants.SchemaPrefixItems] = prefixItems.ToArray();
            }
        }

        if (openApiVersion == OpenApiSpecVersion.OpenApi3_0)
        {
            result.Schema = null;
            result.Id = null;
            result.Definitions = null;
            result.If = null;
            result.Then = null;
            result.Else = null;
            result.DependentSchemas = null;
            result.DependentRequired = null;
            result.UnevaluatedPropertiesSchema = null;
        }

        return result;
    }

    private static IDictionary<string, IOpenApiSchema>? ProcessDictionary(
        IDictionary<string, IOpenApiSchema>? schemas,
        JsonElement source,
        string propertyName,
        JsonSerializerOptions options,
        OpenApiSpecVersion openApiVersion)
    {
        if (schemas is null ||
            source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty(propertyName, out var sourceSchemas) ||
            sourceSchemas.ValueKind != JsonValueKind.Object)
        {
            return schemas;
        }

        var result = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        foreach (var (name, schema) in schemas)
        {
            result[name] = sourceSchemas.TryGetProperty(name, out var sourceSchema)
                ? ProcessSchemaValue(schema, sourceSchema, options, openApiVersion)
                : schema;
        }
        return result;
    }

    private static IList<IOpenApiSchema>? ProcessList(
        IList<IOpenApiSchema>? schemas,
        JsonElement source,
        string propertyName,
        JsonSerializerOptions options,
        OpenApiSpecVersion openApiVersion)
    {
        if (schemas is null ||
            source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty(propertyName, out var sourceSchemas) ||
            sourceSchemas.ValueKind != JsonValueKind.Array)
        {
            return schemas;
        }

        var sourceElements = sourceSchemas.EnumerateArray().ToArray();
        var result = new List<IOpenApiSchema>(schemas.Count);
        for (var i = 0; i < schemas.Count; i++)
        {
            result.Add(i < sourceElements.Length
                ? ProcessSchemaValue(schemas[i], sourceElements[i], options, openApiVersion)
                : schemas[i]);
        }
        return result;
    }

    private static IOpenApiSchema? ProcessChild(
        IOpenApiSchema? schema,
        JsonElement source,
        string propertyName,
        JsonSerializerOptions options,
        OpenApiSpecVersion openApiVersion)
    {
        if (schema is null ||
            source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty(propertyName, out var sourceSchema))
        {
            return schema;
        }

        return ProcessSchemaValue(schema, sourceSchema, options, openApiVersion);
    }

    private static IOpenApiSchema ProcessSchemaValue(
        IOpenApiSchema schema,
        JsonElement source,
        JsonSerializerOptions options,
        OpenApiSpecVersion openApiVersion)
        => schema is OpenApiSchema concrete
            ? ProcessSchema(concrete, source, options, openApiVersion)
            : schema;

    private static OpenApiSchema CreateRoot(string identity)
    {
        var result = new OpenApiJsonSchema.OpenApiJsonSchemaModel
        {
            Metadata = new Dictionary<string, object>
            {
                [OpenApiConstants.SchemaValidatedIdentity] = identity,
            },
        };
        return result;
    }

    private static OpenApiSchema ParseSchema(ReadOnlySpan<byte> utf8Schema, JsonSerializerOptions options)
    {
        var reader = new Utf8JsonReader(utf8Schema);
        var converter = new OpenApiSchemaJsonConverter(OpenApiSpecVersion.OpenApi3_1);
        return converter.Read(ref reader, typeof(OpenApiSchema), options) ??
            throw new JsonException(Resources.ValidatedJsonSchemaMustBeSchema);
    }

    private static OpenApiSchema ParseSchema(JsonElement schema, JsonSerializerOptions options)
    {
        var utf8Schema = Encoding.UTF8.GetBytes(schema.GetRawText());
        return ParseSchema(utf8Schema, options);
    }

    private static bool ContainsReference(JsonElement current)
    {
        if (current.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in current.EnumerateObject())
            {
                if (property.NameEquals("$ref") || ContainsReference(property.Value))
                {
                    return true;
                }
            }
        }
        else if (current.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in current.EnumerateArray())
            {
                if (ContainsReference(element))
                {
                    return true;
                }
            }
        }
        return false;
    }

}
