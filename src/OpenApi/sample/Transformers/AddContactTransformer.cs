// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Sample.Transformers;

public sealed class AddContactTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Contact = new OpenApiContact
        {
            Name = "OpenAPI Enthusiast",
            Email = "iloveopenapi@example.com"
        };
        return Task.CompletedTask;
    }
}
