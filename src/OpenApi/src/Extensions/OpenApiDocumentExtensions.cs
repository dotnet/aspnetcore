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
    /// <param name="schemaReference">An <see cref="IOpenApiSchema"/> with a reference to the stored schema.</param>
    /// <returns>Whether the schema was added or already existed.</returns>
    public static bool AddOpenApiSchemaByReference(this OpenApiDocument document, string schemaId, IOpenApiSchema schema, out OpenApiSchemaReference schemaReference)
    {
        // Make sure the document has a workspace,
        // AddComponent will add it to the workspace when adding the component.
        document.Workspace ??= new();
        // AddComponent will only add the schema if it doesn't already exist.
        var schemaAdded = document.AddComponent(schemaId, schema);

        object? description = null;
        object? example = null;
        object? defaultAnnotation = null;
        if (schema is OpenApiSchema { Metadata: not null } actualSchema)
        {
            actualSchema.Metadata.TryGetValue(OpenApiConstants.RefDescriptionAnnotation, out description);
            actualSchema.Metadata.TryGetValue(OpenApiConstants.RefExampleAnnotation, out example);
            actualSchema.Metadata.TryGetValue(OpenApiConstants.RefDefaultAnnotation, out defaultAnnotation);
        }

        schemaReference = new OpenApiSchemaReference(schemaId, document)
        {
            Description = description as string,
            Examples = example is JsonNode exampleJson ? [exampleJson] : null,
            Default = defaultAnnotation as JsonNode,
        };

        return schemaAdded;
    }
}
