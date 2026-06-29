// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class TypeBasedDocumentInitializer : IDocumentInitializer
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _initializerType;
    private readonly ObjectFactory _initializerFactory;

    internal TypeBasedDocumentInitializer([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type initializerType)
    {
        _initializerType = initializerType;
        _initializerFactory = ActivatorUtilities.CreateFactory(_initializerType, []);
    }

    public async Task InitializeAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var initializer = _initializerFactory.Invoke(context.ApplicationServices, []) as IDocumentInitializer;
        Debug.Assert(initializer is not null, $"The type {_initializerType} does not implement {nameof(IDocumentInitializer)}.");
        try
        {
            await initializer.InitializeAsync(document, context, cancellationToken);
        }
        finally
        {
            if (initializer is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (initializer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
