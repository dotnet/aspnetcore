// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class DelegateOpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    private readonly Func<OpenApiSchema, OpenApiSchemaTransformerContext, CancellationToken, Task> _transformer;

    public DelegateOpenApiSchemaTransformer(Func<OpenApiSchema, OpenApiSchemaTransformerContext, CancellationToken, Task> transformer)
    {
        _transformer = transformer;
    }

    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        await _transformer(schema, context, cancellationToken);
    }
}
