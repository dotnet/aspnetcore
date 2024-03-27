// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class DelegateOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
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
                foreach (var operation in pathItem.Operations.Values)
                {
                    var descriptionId = ((OpenApiString)operation.Extensions[OpenApiConstants.DescriptionId]).Value;
                    var operationContext = documentService.OperationTransformerContextCache[descriptionId];
                    await _operationTransformer(operation, operationContext, cancellationToken);
                }
            }
        }
    }
}
