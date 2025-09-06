// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.OpenApi.Services.Schemas.Transformers;

internal class AttributeAnnotationsSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        schema.Metadata ??= new Dictionary<string, object>();
        var isInlinedSchema = !schema.Metadata.ContainsKey(OpenApiConstants.SchemaId) || string.IsNullOrEmpty(schema.Metadata[OpenApiConstants.SchemaId] as string);
        if (context.JsonTypeInfo.Type.GetCustomAttributes(inherit: false).OfType<DescriptionAttribute>().LastOrDefault() is { } typeDescriptionAttribute)
        {
            schema.Description = typeDescriptionAttribute.Description;
        }

        if (context.JsonPropertyInfo?.AttributeProvider?.GetCustomAttributes(inherit: false).OfType<DescriptionAttribute>().LastOrDefault() is { } propertyDescriptionAttribute)
        {
            if (isInlinedSchema)
            {
                schema.Description = propertyDescriptionAttribute.Description;
            }
            else
            {
                schema.Metadata![OpenApiConstants.RefDescriptionAnnotation] = propertyDescriptionAttribute.Description;
            }
        }

        if (context.JsonTypeInfo.Type.GetCustomAttributes(inherit: false).OfType<DefaultValueAttribute>().LastOrDefault() is { } typeDefaultValueAttribute)
        {
            schema.Default = GetDefaultValueAsJsonNode(typeDefaultValueAttribute, context.JsonTypeInfo);
        }

        if (context.JsonPropertyInfo?.AttributeProvider?.GetCustomAttributes(inherit: false).OfType<DefaultValueAttribute>().LastOrDefault() is { } propertyDefaultValueAttribute)
        {
            var defaultValueJson = GetDefaultValueAsJsonNode(propertyDefaultValueAttribute, context.JsonTypeInfo);
            if (isInlinedSchema)
            {
                schema.Default = defaultValueJson;
            }
            else
            {
                schema.Metadata![OpenApiConstants.RefDefaultAnnotation] = defaultValueJson!;
            }
        }

        return Task.CompletedTask;
    }

    private static JsonNode? GetDefaultValueAsJsonNode(DefaultValueAttribute defaultValueAttribute, JsonTypeInfo jsonTypeInfo)
    {
        if(defaultValueAttribute.Value is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(defaultValueAttribute.Value, jsonTypeInfo);
    }
}
