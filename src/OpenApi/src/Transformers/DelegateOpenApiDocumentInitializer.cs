// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class DelegateOpenApiDocumentInitializer(Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task> initializer) : IOpenApiDocumentInitializer
{
    public Task InitializeAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        => initializer(document, context, cancellationToken);
}
