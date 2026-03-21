// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Supports managing elements that belong in the "components" section of
/// an OpenAPI document. In particular, this is the API that is used to
/// interact with the JSON schemas that are managed by a given OpenAPI document.
/// </summary>
internal sealed class OpenApiSchemaService(
    [ServiceKey] string documentName,
    IOptions<JsonOptions> jsonOptions,
    IOptionsMonitor<OpenApiOptions> optionsMonitor)
{
    private readonly ConcurrentDictionary<Type, string?> _schemaIdCache = new();
    private readonly OpenApiJsonSchemaContext _jsonSchemaContext = new(new(jsonOptions.Value.SerializerOptions));
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(jsonOptions.Value.SerializerOptions)
    {
        // In order to properly handle the `RequiredAttribute` on type properties, add a modifier to support
        // setting `JsonPropertyInfo.IsRequired` based on the presence of the `RequiredAttribute`.
        TypeInfoResolver = jsonOptions.Value.SerializerOptions.TypeInfoResolver?.WithAddedModifier(jsonTypeInfo =>
        {
            if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return;
            }
            foreach (var propertyInfo in jsonTypeInfo.Properties)
            {
                var hasRequiredAttribute = propertyInfo.AttributeProvider?
                    .GetCustomAttributes(inherit: false)
                    .Any(attr => attr is RequiredAttribute);
                propertyInfo.IsRequired |= hasRequiredAttribute ?? false;
            }
        })
    };

    private readonly JsonSchemaExporterOptions _configuration = new()
    {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = (context, schema) =>
        {
            var type = context.TypeInfo.Type;
            // Fix up schemas generated for IFormFile, IFormFileCollection, Stream, PipeReader and FileContentResult
            // that appear as properties within complex types.
            if (type == typeof(IFormFile) || type == typeof(Stream) || type == typeof(PipeReader) || type == typeof(Mvc.FileContentResult))
            {
                schema = new JsonObject
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                    [OpenApiSchemaKeywords.FormatKeyword] = "binary",
                    [OpenApiConstants.SchemaId] = "IFormFile"
                };
            }
            else if (type == typeof(IFormFileCollection))
            {
                schema = new JsonObject
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "array",
                    [OpenApiSchemaKeywords.ItemsKeyword] = new JsonObject
                    {
                        [OpenApiSchemaKeywords.TypeKeyword] = "string",
                        [OpenApiSchemaKeywords.FormatKeyword] = "binary",
                        [OpenApiConstants.SchemaId] = "IFormFile"
                    }
                };
            }
            else if (type.IsJsonPatchDocument())
            {
                schema = CreateSchemaForJsonPatch();
            }
            // STJ uses `true` in place of an empty object to represent a schema that matches
            // anything (like the `object` type) or types with user-defined converters. We override
            // this default behavior here to match the format expected in OpenAPI v3.
            if (schema.GetValueKind() == JsonValueKind.True)
            {
                schema = new JsonObject();
            }
            var createSchemaReferenceId = optionsMonitor.Get(documentName).CreateSchemaReferenceId;
            schema.ApplyPrimitiveTypesAndFormats(context, createSchemaReferenceId);
            schema.ApplySchemaReferenceId(context, createSchemaReferenceId);
            schema.MapPolymorphismOptionsToDiscriminator(context, createSchemaReferenceId);
            if (context.PropertyInfo is { } jsonPropertyInfo)
            {
                schema.ApplyNullabilityContextInfo(jsonPropertyInfo);
            }
            if (context.TypeInfo.Type.GetCustomAttributes(inherit: false).OfType<DescriptionAttribute>().LastOrDefault() is { } typeDescriptionAttribute)
            {
                schema[OpenApiSchemaKeywords.DescriptionKeyword] = typeDescriptionAttribute.Description;
            }
            if (context.PropertyInfo is { AttributeProvider: { } attributeProvider })
            {
                var propertyAttributes = attributeProvider.GetCustomAttributes(inherit: false);
                if (propertyAttributes.OfType<ValidationAttribute>() is { } validationAttributes)
                {
                    schema.ApplyValidationAttributes(validationAttributes);
                }
                if (propertyAttributes.OfType<DefaultValueAttribute>().LastOrDefault() is { } defaultValueAttribute)
                {
                    schema.ApplyDefaultValue(defaultValueAttribute.Value, context.TypeInfo);
                }
                var isInlinedSchema = !schema.WillBeComponentized();
                if (isInlinedSchema)
                {
                    if (propertyAttributes.OfType<DescriptionAttribute>().LastOrDefault() is { } descriptionAttribute)
                    {
                        schema[OpenApiSchemaKeywords.DescriptionKeyword] = descriptionAttribute.Description;
                    }
                }
                else
                {
                    if (propertyAttributes.OfType<DescriptionAttribute>().LastOrDefault() is { } descriptionAttribute)
                    {
                        schema[OpenApiConstants.RefDescriptionAnnotation] = descriptionAttribute.Description;
                    }
                }
            }
            schema.PruneNullTypeForComponentizedTypes();
            return schema;
        }
    };

    private static JsonObject CreateSchemaForJsonPatch()
    {
        var addReplaceTest = new JsonObject()
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.AdditionalPropertiesKeyword] = false,
            [OpenApiSchemaKeywords.RequiredKeyword] = JsonArray(["op", "path", "value"]),
            [OpenApiSchemaKeywords.PropertiesKeyword] = new JsonObject
            {
                ["op"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                    [OpenApiSchemaKeywords.EnumKeyword] = JsonArray(["add", "replace", "test"]),
                },
                ["path"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string"
                },
                ["value"] = new JsonObject()
            }
        };

        var moveCopy = new JsonObject()
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.AdditionalPropertiesKeyword] = false,
            [OpenApiSchemaKeywords.RequiredKeyword] = JsonArray(["op", "path", "from"]),
            [OpenApiSchemaKeywords.PropertiesKeyword] = new JsonObject
            {
                ["op"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                    [OpenApiSchemaKeywords.EnumKeyword] = JsonArray(["move", "copy"]),
                },
                ["path"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string"
                },
                ["from"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string"
                },
            }
        };

        var remove = new JsonObject()
        {
            [OpenApiSchemaKeywords.TypeKeyword] = "object",
            [OpenApiSchemaKeywords.AdditionalPropertiesKeyword] = false,
            [OpenApiSchemaKeywords.RequiredKeyword] = JsonArray(["op", "path"]),
            [OpenApiSchemaKeywords.PropertiesKeyword] = new JsonObject
            {
                ["op"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                    [OpenApiSchemaKeywords.EnumKeyword] = JsonArray(["remove"])
                },
                ["path"] = new JsonObject()
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string"
                },
            }
        };

        return new JsonObject
        {
            [OpenApiConstants.SchemaId] = "JsonPatchDocument",
            [OpenApiSchemaKeywords.TypeKeyword] = "array",
            [OpenApiSchemaKeywords.ItemsKeyword] = new JsonObject
            {
                [OpenApiSchemaKeywords.OneOfKeyword] = JsonArray([addReplaceTest, moveCopy, remove])
            },
        };

        // Using JsonArray inline causes the compile to pick the generic Add<T>() overload
        // which then generates native AoT warnings without adding a cost. To Avoid that use
        // this helper method that uses JsonNode to pick the native AoT compatible overload instead.
        static JsonArray JsonArray(ReadOnlySpan<JsonNode> values)
        {
            var array = new JsonArray();

            foreach (var value in values)
            {
                array.Add(value);
            }

            return array;
        }
    }

    internal async Task<OpenApiSchema> GetOrCreateUnresolvedSchemaAsync(OpenApiDocument? document, Type type, IServiceProvider scopedServiceProvider, IOpenApiSchemaTransformer[] schemaTransformers, ApiParameterDescription? parameterDescription = null, CancellationToken cancellationToken = default)
    {
        var schemaAsJsonObject = CreateSchema(type);
        if (parameterDescription is not null)
        {
            schemaAsJsonObject.ApplyParameterInfo(parameterDescription, _jsonSerializerOptions.GetTypeInfo(type));
        }
        // Use _jsonSchemaContext constructed from _jsonSerializerOptions to respect shared config set by end-user,
        // particularly in the case of maxDepth.
        var deserializedSchema = JsonSerializer.Deserialize(schemaAsJsonObject, _jsonSchemaContext.OpenApiJsonSchema);
        Debug.Assert(deserializedSchema != null, "The schema should have been deserialized successfully and materialize a non-null value.");
        var schema = deserializedSchema.Schema;
        await ApplySchemaTransformersAsync(document, schema, type, scopedServiceProvider, schemaTransformers, parameterDescription, cancellationToken);
        return schema;
    }

    internal async Task<IOpenApiSchema> GetOrCreateSchemaAsync(OpenApiDocument document, Type type, IServiceProvider scopedServiceProvider, IOpenApiSchemaTransformer[] schemaTransformers, ApiParameterDescription? parameterDescription = null, CancellationToken cancellationToken = default)
    {
        var schema = await GetOrCreateUnresolvedSchemaAsync(document, type, scopedServiceProvider, schemaTransformers, parameterDescription, cancellationToken);

        // Cache the root schema IDs since we expect to be called
        // on the same type multiple times within an API
        var baseSchemaId = _schemaIdCache.GetOrAdd(type, t =>
        {
            var jsonTypeInfo = _jsonSerializerOptions.GetTypeInfo(t);
            return optionsMonitor.Get(documentName).CreateSchemaReferenceId(jsonTypeInfo);
        });

        return ResolveReferenceForSchema(document, schema, baseSchemaId);
    }

    internal static IOpenApiSchema ResolveReferenceForSchema(OpenApiDocument document, IOpenApiSchema inputSchema, string? rootSchemaId, string? baseSchemaId = null)
    {
        var schema = UnwrapOpenApiSchema(inputSchema);

        var isComponentizedSchema = schema.IsComponentizedSchema(out var schemaId);

        // When we register it, this will be the resulting reference
        OpenApiSchemaReference? resultSchemaReference = null;
        if (inputSchema is OpenApiSchema && isComponentizedSchema)
        {
            var targetReferenceId = baseSchemaId is not null
                ? $"{baseSchemaId}{schemaId}"
                : schemaId;
            if (!string.IsNullOrEmpty(targetReferenceId))
            {
                if (!document.AddOpenApiSchemaByReference(targetReferenceId, schema, out resultSchemaReference))
                {
                    // We already added this schema, so it has already been resolved.
                    return resultSchemaReference;
                }
            }
        }

        if (schema.AnyOf is { Count: > 0 })
        {
            for (var i = 0; i < schema.AnyOf.Count; i++)
            {
                schema.AnyOf[i] = ResolveReferenceForSchema(document, schema.AnyOf[i], rootSchemaId, schemaId);
            }
        }

        if (schema.Properties is not null)
        {
            foreach (var property in schema.Properties)
            {
                var resolvedProperty = ResolveReferenceForSchema(document, property.Value, rootSchemaId);
                if (property.Value is OpenApiSchema targetSchema &&
                    targetSchema.Metadata?.TryGetValue(OpenApiConstants.NullableProperty, out var isNullableProperty) == true &&
                    isNullableProperty is true)
                {
                    schema.Properties[property.Key] = resolvedProperty.CreateOneOfNullableWrapper();
                }
                else
                {
                    schema.Properties[property.Key] = resolvedProperty;
                }
            }
        }

        if (schema.AllOf is { Count: > 0 })
        {
            for (var i = 0; i < schema.AllOf.Count; i++)
            {
                schema.AllOf[i] = ResolveReferenceForSchema(document, schema.AllOf[i], rootSchemaId);
            }
        }

        if (schema.OneOf is { Count: > 0 })
        {
            for (var i = 0; i < schema.OneOf.Count; i++)
            {
                schema.OneOf[i] = ResolveReferenceForSchema(document, schema.OneOf[i], rootSchemaId);
            }
        }

        if (schema.AdditionalProperties is not null)
        {
            schema.AdditionalProperties = ResolveReferenceForSchema(document, schema.AdditionalProperties, rootSchemaId);
        }

        if (schema.Items is not null)
        {
            schema.Items = ResolveReferenceForSchema(document, schema.Items, rootSchemaId);
        }

        if (schema.Not is not null)
        {
            schema.Not = ResolveReferenceForSchema(document, schema.Not, rootSchemaId);
        }

        if (resultSchemaReference is not null)
        {
            return resultSchemaReference;
        }

        return schema;
    }

    private static OpenApiSchema UnwrapOpenApiSchema(IOpenApiSchema sourceSchema)
    {
        if (sourceSchema is OpenApiSchemaReference schemaReference)
        {
            if (schemaReference.Target is OpenApiSchema target)
            {
                return target;
            }
            else
            {
                throw new InvalidOperationException($"The input schema must be an {nameof(OpenApiSchema)} or {nameof(OpenApiSchemaReference)}.");
            }
        }
        else if (sourceSchema is OpenApiSchema directSchema)
        {
            return directSchema;
        }
        else
        {
            throw new InvalidOperationException($"The input schema must be an {nameof(OpenApiSchema)} or {nameof(OpenApiSchemaReference)}.");
        }
    }

    internal async Task ApplySchemaTransformersAsync(OpenApiDocument? document, IOpenApiSchema schema, Type type, IServiceProvider scopedServiceProvider, IOpenApiSchemaTransformer[] schemaTransformers, ApiParameterDescription? parameterDescription = null, CancellationToken cancellationToken = default)
    {
        if (schemaTransformers.Length == 0)
        {
            return;
        }
        var jsonTypeInfo = _jsonSerializerOptions.GetTypeInfo(type);
        var context = new OpenApiSchemaTransformerContext
        {
            DocumentName = documentName,
            JsonTypeInfo = jsonTypeInfo,
            JsonPropertyInfo = null,
            ParameterDescription = parameterDescription,
            ApplicationServices = scopedServiceProvider,
            Document = document,
            SchemaTransformers = schemaTransformers
        };
        for (var i = 0; i < schemaTransformers.Length; i++)
        {
            // Reset context object to base state before running each transformer.
            var transformer = schemaTransformers[i];
            await InnerApplySchemaTransformersAsync(schema, jsonTypeInfo, null, context, transformer, cancellationToken);
        }
    }

    private async Task InnerApplySchemaTransformersAsync(IOpenApiSchema inputSchema,
        JsonTypeInfo jsonTypeInfo,
        JsonPropertyInfo? jsonPropertyInfo,
        OpenApiSchemaTransformerContext context,
        IOpenApiSchemaTransformer transformer,
        CancellationToken cancellationToken = default)
    {
        context.UpdateJsonTypeInfo(jsonTypeInfo, jsonPropertyInfo);
        var schema = UnwrapOpenApiSchema(inputSchema);
        await transformer.TransformAsync(schema, context, cancellationToken);

        // Only apply transformers on polymorphic schemas where we can resolve the derived
        // types associated with the base type.
        if (schema.AnyOf is { Count: > 0 } && jsonTypeInfo.PolymorphismOptions is not null)
        {
            var anyOfIndex = 0;
            foreach (var derivedType in jsonTypeInfo.PolymorphismOptions.DerivedTypes)
            {
                var derivedJsonTypeInfo = _jsonSerializerOptions.GetTypeInfo(derivedType.DerivedType);
                if (schema.AnyOf.Count <= anyOfIndex)
                {
                    break;
                }
                await InnerApplySchemaTransformersAsync(schema.AnyOf[anyOfIndex], derivedJsonTypeInfo, null, context, transformer, cancellationToken);
                anyOfIndex++;
            }
        }

        // If the schema is an array but uses AnyOf or OneOf then ElementType is null
        if (schema.Items is not null && jsonTypeInfo.ElementType is not null)
        {
            var elementTypeInfo = _jsonSerializerOptions.GetTypeInfo(jsonTypeInfo.ElementType);
            await InnerApplySchemaTransformersAsync(schema.Items, elementTypeInfo, null, context, transformer, cancellationToken);
        }

        if (schema.Properties is { Count: > 0 })
        {
            foreach (var propertyInfo in jsonTypeInfo.Properties)
            {
                if (schema.Properties.TryGetValue(propertyInfo.Name, out var propertySchema))
                {
                    await InnerApplySchemaTransformersAsync(propertySchema, _jsonSerializerOptions.GetTypeInfo(propertyInfo.PropertyType), propertyInfo, context, transformer, cancellationToken);
                }
            }
        }

        if (schema is { AdditionalPropertiesAllowed: true, AdditionalProperties: not null } &&
            jsonTypeInfo.ElementType is not null)
        {
            var elementTypeInfo = _jsonSerializerOptions.GetTypeInfo(jsonTypeInfo.ElementType);
            await InnerApplySchemaTransformersAsync(schema.AdditionalProperties, elementTypeInfo, null, context, transformer, cancellationToken);
        }
    }

    private JsonNode CreateSchema(Type type)
    {
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(_jsonSerializerOptions, type, _configuration);
        return ResolveReferences(schema, schema);
    }

    private static JsonNode ResolveReferences(JsonNode node, JsonNode rootSchema)
    {
        return ResolveReferencesRecursive(node, rootSchema);
    }

    private static JsonNode ResolveReferencesRecursive(JsonNode node, JsonNode rootSchema)
    {
        if (node is JsonObject jsonObject)
        {
            if (jsonObject.TryGetPropertyValue(OpenApiConstants.RefKeyword, out var refNode) &&
                refNode is JsonValue refValue &&
                refValue.TryGetValue<string>(out var refString) &&
                refString.StartsWith(OpenApiConstants.RefPrefix, StringComparison.Ordinal))
            {
                try
                {
                    // Resolve the reference path to the actual schema content
                    // to avoid relative references
                    var resolvedNode = ResolveReference(refString, rootSchema);
                    if (resolvedNode != null)
                    {
                        return resolvedNode.DeepClone();
                    }
                }
                catch (InvalidOperationException)
                {
                    // If resolution fails due to invalid path, return the original reference
                    // This maintains backward compatibility while preventing crashes
                }

                // If resolution fails, return the original reference
                return node;
            }

            // Process all properties recursively
            var newObject = new JsonObject();
            foreach (var property in jsonObject)
            {
                if (property.Value != null)
                {
                    var processedValue = ResolveReferencesRecursive(property.Value, rootSchema);
                    newObject[property.Key] = processedValue?.DeepClone();
                }
                else
                {
                    newObject[property.Key] = null;
                }
            }
            return newObject;
        }
        else if (node is JsonArray jsonArray)
        {
            var newArray = new JsonArray();
            for (var i = 0; i < jsonArray.Count; i++)
            {
                if (jsonArray[i] != null)
                {
                    var processedValue = ResolveReferencesRecursive(jsonArray[i]!, rootSchema);
                    newArray.Add(processedValue?.DeepClone());
                }
                else
                {
                    newArray.Add(null);
                }
            }
            return newArray;
        }

        // Return non-$ref nodes as-is
        return node;
    }

    private static JsonNode? ResolveReference(string refPath, JsonNode rootSchema)
    {
        if (string.IsNullOrWhiteSpace(refPath))
        {
            throw new InvalidOperationException("Reference path cannot be null or empty.");
        }

        if (!refPath.StartsWith(OpenApiConstants.RefPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Only fragment references (starting with '{OpenApiConstants.RefPrefix}') are supported. Found: {refPath}");
        }

        var path = refPath.TrimStart('#', '/');
        if (string.IsNullOrEmpty(path))
        {
            return rootSchema;
        }

        var segments = path.Split('/');
        var current = rootSchema;

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (current is JsonObject currentObject)
            {
                if (currentObject.TryGetPropertyValue(segment, out var nextNode) && nextNode != null)
                {
                    current = nextNode;
                }
                else
                {
                    var partialPath = string.Join('/', segments.Take(i + 1));
                    throw new InvalidOperationException($"Failed to resolve reference '{refPath}': path segment '{segment}' not found at '#{partialPath}'");
                }
            }
            else
            {
                var partialPath = string.Join('/', segments.Take(i));
                throw new InvalidOperationException($"Failed to resolve reference '{refPath}': cannot navigate beyond '#{partialPath}' - expected object but found {current?.GetType().Name ?? "null"}");
            }
        }

        return current;
    }
}
