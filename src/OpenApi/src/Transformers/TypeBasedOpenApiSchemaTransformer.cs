// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class TypeBasedOpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _transformerType;
    private readonly ObjectFactory _transformerFactory;

    internal TypeBasedOpenApiSchemaTransformer([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type transformerType)
    {
        _transformerType = transformerType;
        _transformerFactory = ActivatorUtilities.CreateFactory(_transformerType, []);
    }

    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var transformer = _transformerFactory.Invoke(context.ApplicationServices, []) as IOpenApiSchemaTransformer;
        Debug.Assert(transformer != null, $"The type {_transformerType} does not implement {nameof(IOpenApiSchemaTransformer)}.");
        try
        {
            await transformer.TransformAsync(schema, context, cancellationToken);
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
