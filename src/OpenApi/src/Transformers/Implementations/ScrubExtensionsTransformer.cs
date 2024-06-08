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
            }
        }
        return Task.CompletedTask;
    }
}
