// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class TypeBasedOpenApiDocumentTransformer(Type transformerType) : IOpenApiDocumentTransformer
{
    private readonly ObjectFactory _transformerFactory = ActivatorUtilities.CreateFactory(transformerType, []);
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var transformer = _transformerFactory.Invoke(context.ApplicationServices, []) as IOpenApiDocumentTransformer;
        Debug.Assert(transformer != null, $"The type {transformerType} does not implement {nameof(IOpenApiDocumentTransformer)}.");
        return transformer.TransformAsync(document, context, cancellationToken);
    }
}
