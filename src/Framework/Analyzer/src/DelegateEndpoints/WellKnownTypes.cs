// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

internal sealed class WellKnownTypes
{
    public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = default;
        const string DelegateEndpointRouteBuilderExtensions = "Microsoft.AspNetCore.Builder.DelegateEndpointRouteBuilderExtensions";
        if (compilation.GetTypeByMetadataName(DelegateEndpointRouteBuilderExtensions) is not { } delegateEndpointRouteBuilderExtensions)
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

        wellKnownTypes = new WellKnownTypes
        {
            DelegateEndpointRouteBuilderExtensions = delegateEndpointRouteBuilderExtensions,
            IBinderTypeProviderMetadata = ibinderTypeProviderMetadata,
            BindAttribute = bindAttribute,
        };

        return true;
    }

    public ITypeSymbol DelegateEndpointRouteBuilderExtensions { get; private init; }
    public INamedTypeSymbol IBinderTypeProviderMetadata { get; private init; }
    public INamedTypeSymbol BindAttribute { get; private init; }
}
