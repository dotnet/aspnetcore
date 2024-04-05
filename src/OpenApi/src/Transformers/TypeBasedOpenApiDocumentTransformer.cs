// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class TypeBasedOpenApiDocumentTransformer(Type transformerType) : IOpenApiDocumentTransformer
{
    private readonly ObjectFactory _transformerFactory = ActivatorUtilities.CreateFactory(transformerType, []);
    private IOpenApiDocumentTransformer? _transformer;

    public ValueTask DisposeAsync()
    {
        if (_transformer is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }
        if (_transformer is IDisposable disposable)
        {
            disposable.Dispose();
        }
        return ValueTask.CompletedTask;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        _transformer = _transformerFactory.Invoke(context.ApplicationServices, []) as IOpenApiDocumentTransformer;
        Debug.Assert(_transformer != null, $"The type {transformerType} does not implement {nameof(IOpenApiDocumentTransformer)}.");
        return _transformer.TransformAsync(document, context, cancellationToken);
    }
}
