// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class ActivatedOpenApiDocumentTransformer(Type transformerType) : IOpenApiDocumentTransformer
{
    internal IOpenApiDocumentTransformer? Transformer { get; set; }

    internal void Initialize(IServiceProvider serviceProvider)
    {
        Transformer ??= ActivatorUtilities.CreateInstance(serviceProvider, transformerType) as IOpenApiDocumentTransformer;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        Debug.Assert(Transformer != null, "Transformer should have been initialized.");
        return Transformer.TransformAsync(document, context, cancellationToken);
    }
}
