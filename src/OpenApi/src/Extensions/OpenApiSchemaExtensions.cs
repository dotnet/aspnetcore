// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

    public static void MakeArrayItemsNullable(this IOpenApiSchema schema)
    {
        if (schema is not OpenApiSchema { Items: { } items } arraySchema || items.IsAlreadyNullable())
        {
            return;
        }

        if (items is OpenApiSchema { Type: { } itemType } inlineItemSchema)
        {
            inlineItemSchema.Type = itemType | JsonSchemaType.Null;
        }
        else
        {
            arraySchema.Items = items.CreateOneOfNullableWrapper();
        }
    }

    private static bool IsAlreadyNullable(this IOpenApiSchema schema)
    {
        // Use the IOpenApiSchema interface members directly (rather than pattern-matching on
        // OpenApiSchema) so that schema references (e.g. "$ref" to a componentized schema) are
        // also handled correctly, since they proxy these properties to their target schema.
        if (schema.Type is { } schemaType && schemaType.HasFlag(JsonSchemaType.Null))
        {
            return true;
        }

        // An inline enum schema (e.g. for a nullable enum type with no $ref) represents
        // nullability by including a `null` entry in its `enum` list rather than setting
        // the `type` keyword, so check for that case too.
        if (schema.Enum is { } enumValues && enumValues.Any(static value => value is null))
        {
            return true;
        }

        if (schema.OneOf is { } oneOfSchemas)
        {
            foreach (var oneOfSchema in oneOfSchemas)
            {
                if (oneOfSchema.Type is { } oneOfSchemaType && oneOfSchemaType.HasFlag(JsonSchemaType.Null))
                {
                    return true;
                }
            }
        }

        return false;
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
