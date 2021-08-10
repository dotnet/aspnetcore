// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.MinimalActions;

internal sealed class WellKnownTypes
{
    public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = default;
        const string MinimalActionEndpointRouteBuilderExtensions = "Microsoft.AspNetCore.Builder.MinimalActionEndpointRouteBuilderExtensions";
        if (compilation.GetTypeByMetadataName(MinimalActionEndpointRouteBuilderExtensions) is not { } minimalActionEndpointRouteBuilderExtensions)
        {
            return false;
        }

        const string IBinderTypeProviderMetadata = "Microsoft.AspNetCore.Mvc.ModelBinding.IBinderTypeProviderMetadata";
        if (compilation.GetTypeByMetadataName(IBinderTypeProviderMetadata) is not { } ibinderTypeProviderMetadata)
        {
            return false;
        }

        // There isn't a good way to distinguish BindAttribute from other allowed
        // MVC's attributes like From*.
        const string BindAttribute = "Microsoft.AspNetCore.Mvc.BindAttribute";
        if (compilation.GetTypeByMetadataName(BindAttribute) is not { } bindAttribute)
        {
            return false;
        }

        const string IFromRouteMetadata = "Microsoft.AspNetCore.Http.Metadata.IFromRouteMetadata";
        if (compilation.GetTypeByMetadataName(IFromRouteMetadata) is not { } iFromRouteMetadata)
        {
            return false;
        }

        wellKnownTypes = new WellKnownTypes
        {
            MinimalActionEndpointRouteBuilderExtensions = minimalActionEndpointRouteBuilderExtensions,
            IBinderTypeProviderMetadata = ibinderTypeProviderMetadata,
            BindAttribute = bindAttribute,
            IFromRouteMetadata = iFromRouteMetadata,
        };

        return true;
    }

    public ITypeSymbol MinimalActionEndpointRouteBuilderExtensions { get; private init; }
    public INamedTypeSymbol IBinderTypeProviderMetadata { get; private init; }
    public INamedTypeSymbol BindAttribute { get; private init; }
    public INamedTypeSymbol IFromRouteMetadata { get; private init; }
}
