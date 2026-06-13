// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiSchemaExtensions
{
    private static readonly OpenApiSchema _nullSchema = new() { Type = JsonSchemaType.Null };

    public static IOpenApiSchema CreateOneOfNullableWrapper(this IOpenApiSchema originalSchema)
    {
        return new OpenApiSchema
        {
            OneOf =
            [
                _nullSchema,
                originalSchema
            ]
        };
    }

    public static bool IsOneOfNullableWrapper(this OpenApiSchema schema)
    {
        if (schema.OneOf is not { Count: 2 })
        {
            return false;
        }

        var hasNullSchema = false;
        var hasNonNullSchema = false;

        foreach (var item in schema.OneOf)
        {
            if (item is OpenApiSchema { Type: JsonSchemaType.Null })
            {
                hasNullSchema = true;
            }
            else
            {
                hasNonNullSchema = true;
            }
        }

        return hasNullSchema && hasNonNullSchema;
    }

    public static bool IsComponentizedSchema(this OpenApiSchema schema)
        => schema.IsComponentizedSchema(out _);

    public static bool IsComponentizedSchema(this OpenApiSchema schema, [NotNullWhen(true)] out string? schemaId)
    {
        if(schema.Metadata is not null
            && schema.Metadata.TryGetValue(OpenApiConstants.SchemaId, out var schemaIdAsObject)
            && schemaIdAsObject is string schemaIdString
            && !string.IsNullOrEmpty(schemaIdString))
        {
            schemaId = schemaIdString;
            return true;
        }
        schemaId = null;
        return false;
    }

    public static bool IsUnion(this OpenApiSchema schema)
        => schema.Metadata is not null
            && schema.Metadata.TryGetValue(OpenApiConstants.SchemaIsUnion, out var isUnion)
            && isUnion is true;
}
