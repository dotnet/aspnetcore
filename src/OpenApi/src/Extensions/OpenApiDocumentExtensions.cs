// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiDocumentExtensions
{
    /// <summary>
    /// Registers a <see cref="IOpenApiSchema" /> into the top-level components store on the
    /// <see cref="OpenApiDocument" /> and returns a resolvable reference to it.
    /// </summary>
    /// <param name="document">The <see cref="OpenApiDocument"/> to register the schema onto.</param>
    /// <param name="schemaId">The ID that serves as the key for the schema in the schema store.</param>
    /// <param name="schema">The <see cref="IOpenApiSchema" /> to register into the document.</param>
    /// <returns>An <see cref="IOpenApiSchema"/> with a reference to the stored schema.</returns>
    public static IOpenApiSchema AddOpenApiSchemaByReference(this OpenApiDocument document, string schemaId, IOpenApiSchema schema)
    {
        document.Components ??= new();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        document.Components.Schemas[schemaId] = schema;
        document.Workspace ??= new();
        var location = document.BaseUri + "/components/schemas/" + schemaId;
        document.Workspace.RegisterComponentForDocument(document, schema, location);

        object? description = null;
        object? example = null;
        if (schema is OpenApiSchema actualSchema)
        {
            actualSchema.Metadata?.TryGetValue(OpenApiConstants.RefDescriptionAnnotation, out description);
            actualSchema.Metadata?.TryGetValue(OpenApiConstants.RefExampleAnnotation, out example);
        }

        return new OpenApiSchemaReference(schemaId, document)
        {
            Description = description as string,
            Examples = example is JsonNode exampleJson ? [exampleJson] : null,
        };
    }
}
