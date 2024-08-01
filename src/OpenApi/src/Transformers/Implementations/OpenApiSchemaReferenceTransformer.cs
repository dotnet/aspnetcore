// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Document transformer to support mapping duplicate JSON schema instances
/// into JSON schema references across the document.
/// </summary>
internal sealed class OpenApiSchemaReferenceTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var schemaStore = context.ApplicationServices.GetRequiredKeyedService<OpenApiSchemaStore>(context.DocumentName);
        var schemasByReference = schemaStore.SchemasByReference;

        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        foreach (var (schema, referenceId) in schemasByReference.Where(kvp => kvp.Value is not null).OrderBy(kvp => kvp.Value))
        {
            // Reference IDs are only set for schemas that appear  more than once in the OpenAPI
            // document and should be represented as references instead of inlined in the document.
            if (referenceId is not null)
            {
                // Note: we create a copy of the schema here to avoid modifying the original schema
                // so that comparisons between the original schema and the resolved schema during
                // the transformation process are consistent.
                document.Components.Schemas.Add(
                    referenceId,
                    ResolveReferenceForSchema(new OpenApiSchema(schema), schemasByReference, isTopLevel: true));
            }
        }

        foreach (var pathItem in document.Paths.Values)
        {
            for (var i = 0; i < OpenApiConstants.OperationTypes.Length; i++)
            {
                var operationType = OpenApiConstants.OperationTypes[i];
                if (pathItem.Operations.TryGetValue(operationType, out var operation))
                {
                    if (operation.Parameters is not null)
                    {
                        foreach (var parameter in operation.Parameters)
                        {
                            parameter.Schema = ResolveReferenceForSchema(parameter.Schema, schemasByReference);
                        }
                    }

                    if (operation.RequestBody is not null)
                    {
                        foreach (var content in operation.RequestBody.Content)
                        {
                            content.Value.Schema = ResolveReferenceForSchema(content.Value.Schema, schemasByReference);
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
                                    content.Value.Schema = ResolveReferenceForSchema(content.Value.Schema, schemasByReference);
                                }
                            }
                        }
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves the provided schema into a reference if it is found in the schemas-by-reference cache.
    /// </summary>
    /// <param name="schema">The inline schema to replace with a reference.</param>
    /// <param name="schemasByReference">A cache of schemas and their associated reference IDs.</param>
    /// <param name="isTopLevel">When <see langword="true" />, will skip resolving references for the top-most schema provided.</param>
    internal static OpenApiSchema? ResolveReferenceForSchema(OpenApiSchema? schema, Dictionary<OpenApiSchema, string?> schemasByReference, bool isTopLevel = false)
    {
        if (schema is null)
        {
            return schema;
        }

        // If we're resolving schemas for a top-level schema being referenced in the `components.schema` property
        // we don't want to replace the top-level inline schema with a reference to itself. We want to replace
        // inline schemas to reference schemas for all schemas referenced in the top-level schema though (such as
        // `allOf`, `oneOf`, `anyOf`, `items`, `properties`, etc.) which is why `isTopLevel` is only set once.
        if (!isTopLevel && schemasByReference.TryGetValue(schema, out var referenceId) && referenceId is not null)
        {
            return new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = referenceId } };
        }

        if (schema.AllOf is not null)
        {
            for (var i = 0; i < schema.AllOf.Count; i++)
            {
                schema.AllOf[i] = ResolveReferenceForSchema(schema.AllOf[i], schemasByReference);
            }
        }

        if (schema.OneOf is not null)
        {
            for (var i = 0; i < schema.OneOf.Count; i++)
            {
                schema.OneOf[i] = ResolveReferenceForSchema(schema.OneOf[i], schemasByReference);
            }
        }

        if (schema.AnyOf is not null)
        {
            for (var i = 0; i < schema.AnyOf.Count; i++)
            {
                schema.AnyOf[i] = ResolveReferenceForSchema(schema.AnyOf[i], schemasByReference);
            }
        }

        if (schema.AdditionalProperties is not null)
        {
            schema.AdditionalProperties = ResolveReferenceForSchema(schema.AdditionalProperties, schemasByReference);
        }

        if (schema.Items is not null)
        {
            schema.Items = ResolveReferenceForSchema(schema.Items, schemasByReference);
        }

        if (schema.Properties is not null)
        {
            foreach (var property in schema.Properties)
            {
                schema.Properties[property.Key] = ResolveReferenceForSchema(property.Value, schemasByReference);
            }
        }

        if (schema.Not is not null)
        {
            schema.Not = ResolveReferenceForSchema(schema.Not, schemasByReference);
        }
        return schema;
    }
}
