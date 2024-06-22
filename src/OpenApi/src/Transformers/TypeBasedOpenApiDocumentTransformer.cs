// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class TypeBasedOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _transformerType;
    private readonly ObjectFactory _transformerFactory;

    internal TypeBasedOpenApiDocumentTransformer([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type transformerType)
    {
        _transformerType = transformerType;
        _transformerFactory = ActivatorUtilities.CreateFactory(_transformerType, []);
    }

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var transformer = _transformerFactory.Invoke(context.ApplicationServices, []);
        Debug.Assert(transformer is IOpenApiDocumentTransformer or IOpenApiOperationTransformer, $"The type {_transformerType} does not implement one of {nameof(IOpenApiDocumentTransformer)} or {nameof(IOpenApiOperationTransformer)}.");

        try
        {
            if (transformer is IOpenApiDocumentTransformer documentTransformer)
            {
                await documentTransformer.TransformAsync(document, context, cancellationToken);
            }
            else if (transformer is IOpenApiOperationTransformer operationTransformer)
            {
                var documentService = context.ApplicationServices.GetRequiredKeyedService<OpenApiDocumentService>(context.DocumentName);
                await documentService.ForEachOperationAsync(document, operationTransformer.TransformAsync, cancellationToken);
            }
        }
        finally
        {
            if (transformer is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (transformer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
