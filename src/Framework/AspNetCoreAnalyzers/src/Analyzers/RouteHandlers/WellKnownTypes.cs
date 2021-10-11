// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

internal sealed class WellKnownTypes
{
    public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = default;
        const string EndpointRouteBuilderExtensions = "Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions";
        if (compilation.GetTypeByMetadataName(EndpointRouteBuilderExtensions) is not { } endpointRouteBuilderExtensions)
        {
            return false;
        }

        const string Delegate = "System.Delegate";
        if (compilation.GetTypeByMetadataName(Delegate) is not { } @delegate)
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

        const string IResult = "Microsoft.AspNetCore.Http.IResult";
        if (compilation.GetTypeByMetadataName(IResult) is not { } iResult)
        {
            return false;
        }

        const string IActionResult = "Microsoft.AspNetCore.Mvc.IActionResult";
        if (compilation.GetTypeByMetadataName(IActionResult) is not { } iActionResult)
        {
            return false;
        }

        const string IConvertToActionResult = "Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult";
        if (compilation.GetTypeByMetadataName(IConvertToActionResult) is not { } iConvertToActionResult)
        {
            return false;
        }

        wellKnownTypes = new WellKnownTypes
        {
            EndpointRouteBuilderExtensions = endpointRouteBuilderExtensions,
            Delegate = @delegate,
            IBinderTypeProviderMetadata = ibinderTypeProviderMetadata,
            BindAttribute = bindAttribute,
            IResult = iResult,
            IActionResult = iActionResult,
            IConvertToActionResult = iConvertToActionResult,
        };

        return true;
    }

    public INamedTypeSymbol EndpointRouteBuilderExtensions { get; private init; }
    public INamedTypeSymbol Delegate { get; private init; }
    public INamedTypeSymbol IBinderTypeProviderMetadata { get; private init; }
    public INamedTypeSymbol BindAttribute { get; private init; }
    public INamedTypeSymbol IResult { get; private init; }
    public INamedTypeSymbol IActionResult { get; private init; }
    public INamedTypeSymbol IConvertToActionResult { get; private init; }
}
