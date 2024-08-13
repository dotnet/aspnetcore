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

    internal IOpenApiSchemaTransformer InitializeTransformer(IServiceProvider serviceProvider)
    {
        var transformer = _transformerFactory.Invoke(serviceProvider, []) as IOpenApiSchemaTransformer;
        Debug.Assert(transformer != null, $"The type {_transformerType} does not implement {nameof(IOpenApiSchemaTransformer)}.");
        return transformer;
    }

    /// <remarks>
    /// Throw because the activate instance is invoked by the <see cref="OpenApiSchemaService" />.
    /// </remarks>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("This method should not be called. Only activated instances of this transformer should be used.");
}
