// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class DelegateOpenApiOperationTransformer : IOpenApiOperationTransformer
{
    private readonly Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task> _transformer;

    public DelegateOpenApiOperationTransformer(Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task> transformer)
    {
        _transformer = transformer;
    }

    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        await _transformer(operation, context, cancellationToken);
    }
}
