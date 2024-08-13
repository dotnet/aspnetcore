// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class TypeBasedOpenApiOperationTransformer : IOpenApiOperationTransformer
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _transformerType;
    private readonly ObjectFactory _transformerFactory;

    internal TypeBasedOpenApiOperationTransformer([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type transformerType)
    {
        _transformerType = transformerType;
        _transformerFactory = ActivatorUtilities.CreateFactory(_transformerType, []);
    }

    internal IOpenApiOperationTransformer InitializeTransformer(IServiceProvider serviceProvider)
    {
        var transformer = _transformerFactory.Invoke(serviceProvider, []) as IOpenApiOperationTransformer;
        Debug.Assert(transformer != null, $"The type {_transformerType} does not implement {nameof(IOpenApiOperationTransformer)}.");
        return transformer;
    }

    /// <remarks>
    /// Throw because the activate instance is invoked by the <see cref="OpenApiDocumentService" />.
    /// </remarks>
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("This method should not be called. Only activated instances of this transformer should be used.");
}
