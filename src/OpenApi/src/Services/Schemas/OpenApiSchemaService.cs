// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Supports managing elements that belong in the "components" section of
/// an OpenAPI document. In particular, this is the API that is used to
/// interact with the JSON schemas that are managed by a given OpenAPI document.
/// </summary>
internal sealed class OpenApiSchemaService(
    [ServiceKey] string documentName,
    IOptions<JsonOptions> jsonOptions,
    IServiceProvider serviceProvider,
    IOptionsMonitor<OpenApiOptions> optionsMonitor)
{
    private readonly OpenApiSchemaStore _schemaStore = serviceProvider.GetRequiredKeyedService<OpenApiSchemaStore>(documentName);
    private readonly OpenApiOptions _openApiOptions = optionsMonitor.Get(documentName);
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
            // Fix up schemas generated for IFormFile, IFormFileCollection, Stream, and PipeReader
            // that appear as properties within complex types.
            if (type == typeof(IFormFile) || type == typeof(Stream) || type == typeof(PipeReader))
            {
                schema = new JsonObject
                {
                    [OpenApiSchemaKeywords.TypeKeyword] = "string",
                    [OpenApiSchemaKeywords.FormatKeyword] = "binary"
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
                        [OpenApiSchemaKeywords.FormatKeyword] = "binary"
                    }
                };
            }
            // STJ uses `true` in place of an empty object to represent a schema that matches
            // anything. We override this default behavior here to match the style traditionally
            // expected in OpenAPI documents.
            if (type == typeof(object))
            {
                schema = new JsonObject();
            }
            var createSchemaReferenceId = optionsMonitor.Get(documentName).CreateSchemaReferenceId;
            schema.ApplyPrimitiveTypesAndFormats(context, createSchemaReferenceId);
            schema.ApplySchemaReferenceId(context, createSchemaReferenceId);
            schema.ApplyPolymorphismOptions(context, createSchemaReferenceId);
            if (context.PropertyInfo is { } jsonPropertyInfo)
            {
                // Short-circuit STJ's handling of nested properties, which uses a reference to the
                // properties type schema with a schema that uses a document level reference.
                // For example, if the property is a `public NestedTyped Nested { get; set; }` property,
                // "nested": "#/properties/nested" becomes "nested": "#/components/schemas/NestedType"
                if (jsonPropertyInfo.PropertyType == jsonPropertyInfo.DeclaringType)
                {
                    return new JsonObject { [OpenApiSchemaKeywords.RefKeyword] = context.TypeInfo.GetSchemaReferenceId() };
                }
                schema.ApplyNullabilityContextInfo(jsonPropertyInfo);
            }
            if (context.PropertyInfo is { AttributeProvider: { } attributeProvider })
            {
                if (attributeProvider.GetCustomAttributes(inherit: false).OfType<ValidationAttribute>() is { } validationAttributes)
                {
                    schema.ApplyValidationAttributes(validationAttributes);
                }
                if (attributeProvider.GetCustomAttributes(inherit: false).OfType<DefaultValueAttribute>().LastOrDefault() is DefaultValueAttribute defaultValueAttribute)
                {
                    schema.ApplyDefaultValue(defaultValueAttribute.Value, context.TypeInfo);
                }
                if (attributeProvider.GetCustomAttributes(inherit: false).OfType<DescriptionAttribute>().LastOrDefault() is DescriptionAttribute descriptionAttribute)
                {
                    schema[OpenApiSchemaKeywords.DescriptionKeyword] = descriptionAttribute.Description;
                }
            }

            return schema;
        }
    };

    internal async Task<OpenApiSchema> GetOrCreateSchemaAsync(Type type, ApiParameterDescription? parameterDescription = null, bool captureSchemaByRef = false, CancellationToken cancellationToken = default)
    {
        var key = parameterDescription?.ParameterDescriptor is IParameterInfoParameterDescriptor parameterInfoDescription
            && parameterDescription.ModelMetadata.PropertyName is null
            ? new OpenApiSchemaKey(type, parameterInfoDescription.ParameterInfo) : new OpenApiSchemaKey(type, null);
        var schemaAsJsonObject = _schemaStore.GetOrAdd(key, CreateSchema);
        if (parameterDescription is not null)
        {
            schemaAsJsonObject.ApplyParameterInfo(parameterDescription, _jsonSerializerOptions.GetTypeInfo(type));
        }
        var deserializedSchema = JsonSerializer.Deserialize(schemaAsJsonObject, OpenApiJsonSchemaContext.Default.OpenApiJsonSchema);
        Debug.Assert(deserializedSchema != null, "The schema should have been deserialized successfully and materialize a non-null value.");
        var schema = deserializedSchema.Schema;
        await ApplySchemaTransformersAsync(schema, type, parameterDescription, cancellationToken);
        _schemaStore.PopulateSchemaIntoReferenceCache(schema, captureSchemaByRef);
        return schema;
    }

    internal async Task ApplySchemaTransformersAsync(OpenApiSchema schema, Type type, ApiParameterDescription? parameterDescription = null, CancellationToken cancellationToken = default)
    {
        var jsonTypeInfo = _jsonSerializerOptions.GetTypeInfo(type);
        var context = new OpenApiSchemaTransformerContext
        {
            DocumentName = documentName,
            JsonTypeInfo = jsonTypeInfo,
            JsonPropertyInfo = null,
            ParameterDescription = parameterDescription,
            ApplicationServices = serviceProvider
        };
        for (var i = 0; i < _openApiOptions.SchemaTransformers.Count; i++)
        {
            // Reset context object to base state before running each transformer.
            var transformer = _openApiOptions.SchemaTransformers[i];
            // If the transformer is a type-based transformer, we need to initialize and finalize it
            // once in the context of the top-level assembly and not the child properties we are invoking
            // it on.
            if (transformer is TypeBasedOpenApiSchemaTransformer typeBasedTransformer)
            {
                var initializedTransformer = typeBasedTransformer.InitializeTransformer(serviceProvider);
                try
                {
                    await InnerApplySchemaTransformersAsync(schema, jsonTypeInfo, null, context, initializedTransformer, cancellationToken);
                }
                finally
                {
                    await TypeBasedOpenApiSchemaTransformer.FinalizeTransformer(initializedTransformer);
                }
            }
            else
            {
                await InnerApplySchemaTransformersAsync(schema, jsonTypeInfo, null, context, transformer, cancellationToken);
            }
        }
    }

    private async Task InnerApplySchemaTransformersAsync(OpenApiSchema schema,
        JsonTypeInfo jsonTypeInfo,
        JsonPropertyInfo? jsonPropertyInfo,
        OpenApiSchemaTransformerContext context,
        IOpenApiSchemaTransformer transformer,
        CancellationToken cancellationToken = default)
    {
        context.UpdateJsonTypeInfo(jsonTypeInfo, jsonPropertyInfo);
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

        if (schema.Items is not null)
        {
            var elementTypeInfo = _jsonSerializerOptions.GetTypeInfo(jsonTypeInfo.ElementType!);
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
    }

    private JsonNode CreateSchema(OpenApiSchemaKey key)
        => JsonSchemaExporter.GetJsonSchemaAsNode(_jsonSerializerOptions, key.Type, _configuration);
}
