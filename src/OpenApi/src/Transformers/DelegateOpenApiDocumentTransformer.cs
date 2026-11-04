// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class DelegateOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task> _documentTransformer;

    public DelegateOpenApiDocumentTransformer(Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task> transformer)
        => _documentTransformer = transformer;

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        await _documentTransformer(document, context, cancellationToken);
    }
}
