// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

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

    public static bool IsComponentizedSchema(this OpenApiSchema schema)
        => schema.IsComponentizedSchema(out _);

    public static bool IsComponentizedSchema(this OpenApiSchema schema, out string schemaId)
    {
        if(schema.Metadata is not null
            && schema.Metadata.TryGetValue(OpenApiConstants.SchemaId, out var schemaIdAsObject)
            && schemaIdAsObject is string schemaIdString
            && !string.IsNullOrEmpty(schemaIdString))
        {
            schemaId = schemaIdString;
            return true;
        }
        schemaId = string.Empty;
        return false;
    }

    public static OpenApiSchemaReference CreateReference(this OpenApiSchema schema, OpenApiDocument document)
    {
        if (!schema.IsComponentizedSchema(out var schemaId))
        {
            throw new InvalidOperationException("Schema is not a componentized schema.");
        }

        object? description = null;
        object? example = null;
        object? defaultAnnotation = null;
        schema.Metadata?.TryGetValue(OpenApiConstants.RefDescriptionAnnotation, out description);
        schema.Metadata?.TryGetValue(OpenApiConstants.RefExampleAnnotation, out example);
        schema.Metadata?.TryGetValue(OpenApiConstants.RefDefaultAnnotation, out defaultAnnotation);

        return new OpenApiSchemaReference(schemaId, document)
        {
            Description = description as string,
            Examples = example is JsonNode exampleJson ? [exampleJson] : null,
            Default = defaultAnnotation as JsonNode,
        };
    }
}
