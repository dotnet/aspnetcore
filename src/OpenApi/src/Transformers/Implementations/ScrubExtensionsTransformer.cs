// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// A transformer class that removes implementation-specific extension properties
/// from the OpenAPI document.
/// </summary>
internal sealed class ScrubExtensionsTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var pathItem in document.Paths.Values)
        {
            for (var i = 0; i < OpenApiConstants.OperationTypes.Length; i++)
            {
                var operationType = OpenApiConstants.OperationTypes[i];
                if (!pathItem.Operations.TryGetValue(operationType, out var operation))
                {
                    continue;
                }

                operation.Extensions.Remove(OpenApiConstants.DescriptionId);

                if (operation.Parameters is not null)
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        ScrubSchemaIdExtension(parameter.Schema);
                    }
                }

                if (operation.RequestBody is not null)
                {
                    foreach (var content in operation.RequestBody.Content)
                    {
                        ScrubSchemaIdExtension(content.Value.Schema);
                    }
                }

                if (operation.Responses is not null)
                {
                    foreach (var response in operation.Responses.Values)
                    {
                        if (response.Content is not null)
                        {
                            foreach (var content in response.Content)
                            {
                                ScrubSchemaIdExtension(content.Value.Schema);
                            }
                        }
                    }
                }
            }
        }

        foreach (var schema in document.Components.Schemas.Values)
        {
            ScrubSchemaIdExtension(schema);
        }

        return Task.CompletedTask;
    }

    internal static void ScrubSchemaIdExtension(OpenApiSchema? schema)
    {
        if (schema is null)
        {
            return;
        }

        if (schema.AllOf is not null)
        {
            for (var i = 0; i < schema.AllOf.Count; i++)
            {
                ScrubSchemaIdExtension(schema.AllOf[i]);
            }
        }

        if (schema.OneOf is not null)
        {
            for (var i = 0; i < schema.OneOf.Count; i++)
            {
                ScrubSchemaIdExtension(schema.OneOf[i]);
            }
        }

        if (schema.AnyOf is not null)
        {
            for (var i = 0; i < schema.AnyOf.Count; i++)
            {
                ScrubSchemaIdExtension(schema.AnyOf[i]);
            }
        }

        if (schema.AdditionalProperties is not null)
        {
            ScrubSchemaIdExtension(schema.AdditionalProperties);
        }

        if (schema.Items is not null)
        {
            ScrubSchemaIdExtension(schema.Items);
        }

        if (schema.Properties is not null)
        {
            foreach (var property in schema.Properties)
            {
                ScrubSchemaIdExtension(schema.Properties[property.Key]);
            }
        }

        if (schema.Not is not null)
        {
            ScrubSchemaIdExtension(schema.Not);
        }

        schema.Extensions.Remove(OpenApiConstants.SchemaId);
    }
}
