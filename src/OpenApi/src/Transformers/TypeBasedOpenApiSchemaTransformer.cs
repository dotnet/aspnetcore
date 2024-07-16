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
    private IOpenApiSchemaTransformer? _transformer;

    internal TypeBasedOpenApiSchemaTransformer([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type transformerType)
    {
        _transformerType = transformerType;
        _transformerFactory = ActivatorUtilities.CreateFactory(_transformerType, []);
    }

    internal void InitializeTransformer(IServiceProvider serviceProvider)
    {
        _transformer = _transformerFactory.Invoke(serviceProvider, []) as IOpenApiSchemaTransformer;

    }

    internal async Task FinalizeTransformer()
    {
        if (_transformer is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_transformer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        Debug.Assert(_transformer != null, $"The type {_transformerType} does not implement {nameof(IOpenApiSchemaTransformer)}.");
        await _transformer.TransformAsync(schema, context, cancellationToken);

    }
}
