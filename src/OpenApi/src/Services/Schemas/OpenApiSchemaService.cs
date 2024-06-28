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
            schema.ApplyPrimitiveTypesAndFormats(context);
            schema.ApplySchemaReferenceId(context);
            schema.ApplyPolymorphismOptions(context);
            if (context.PropertyInfo is { AttributeProvider: { } attributeProvider } jsonPropertyInfo)
            {
                schema.ApplyNullabilityContextInfo(jsonPropertyInfo);
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

    internal async Task<OpenApiSchema> GetOrCreateSchemaAsync(Type type, ApiParameterDescription? parameterDescription = null, CancellationToken cancellationToken = default)
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
        _schemaStore.PopulateSchemaIntoReferenceCache(schema);
        return schema;
    }

    internal async Task ApplySchemaTransformersAsync(OpenApiSchema schema, Type type, ApiParameterDescription? parameterDescription = null, CancellationToken cancellationToken = default)
    {
        var context = new OpenApiSchemaTransformerContext
        {
            DocumentName = documentName,
            Type = type,
            ParameterDescription = parameterDescription,
            ApplicationServices = serviceProvider
        };
        for (var i = 0; i < _openApiOptions.SchemaTransformers.Count; i++)
        {
            var transformer = _openApiOptions.SchemaTransformers[i];
            await transformer(schema, context, cancellationToken);
        }
    }

    private JsonNode CreateSchema(OpenApiSchemaKey key)
        => JsonSchemaExporter.GetJsonSchemaAsNode(_jsonSerializerOptions, key.Type, _configuration);
}
