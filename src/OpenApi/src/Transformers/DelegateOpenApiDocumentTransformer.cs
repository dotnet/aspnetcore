// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class DelegateOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    // Since there's a finite set of operation types that can be included in a given
    // OpenApiPaths, we can pre-allocate an array of these types and use a direct
    // lookup on the OpenApiPaths dictionary to avoid allocating an enumerator
    // over the KeyValuePairs in OpenApiPaths.
    private static readonly OperationType[] _operationTypes = [
        OperationType.Get,
        OperationType.Post,
        OperationType.Put,
        OperationType.Delete,
        OperationType.Options,
        OperationType.Head,
        OperationType.Patch,
        OperationType.Trace
    ];
    private readonly Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task>? _documentTransformer;
    private readonly Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task>? _operationTransformer;

    public DelegateOpenApiDocumentTransformer(Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task> transformer)
    {
        _documentTransformer = transformer;
    }

    public DelegateOpenApiDocumentTransformer(Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task> transformer)
    {
        _operationTransformer = transformer;
    }

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (_documentTransformer != null)
        {
            await _documentTransformer(document, context, cancellationToken);
        }

        if (_operationTransformer != null)
        {
            var documentService = context.ApplicationServices.GetRequiredKeyedService<OpenApiDocumentService>(context.DocumentName);
            foreach (var pathItem in document.Paths.Values)
            {
                for (var i = 0; i < _operationTypes.Length; i++)
                {
                    var operationType = _operationTypes[i];
                    if (!pathItem.Operations.TryGetValue(operationType, out var operation))
                    {
                        continue;
                    }

                    if (operation.Extensions.TryGetValue(OpenApiConstants.DescriptionId, out var descriptionIdExtension) &&
                        descriptionIdExtension is OpenApiString { Value: var descriptionId } &&
                        documentService.TryGetCachedOperationTransformerContext(descriptionId, out var operationContext))
                    {
                        await _operationTransformer(operation, operationContext, cancellationToken);
                    }
                    else
                    {
                        // If the cached operation transformer context was not found, throw an exception.
                        // This can occur if the `x-aspnetcore-id` extension attribute was removed by the
                        // user in another operation transformer or if the lookup for operation transformer
                        // context resulted in a cache miss. As an alternative here, we could just to implement
                        // the "slow-path" and look up the ApiDescription associated with the OpenApiOperation
                        // using the OperationType and given path, but we'll avoid this for now.
                        throw new InvalidOperationException("Cached operation transformer context not found. Please ensure that the operation contains the `x-aspnetcore-id` extension attribute.");
                    }
                }
            }
        }
    }
}
