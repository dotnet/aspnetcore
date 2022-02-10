// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

internal sealed class WellKnownTypes
{
    public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = default;

        const string ControllerBaseInstance = "Microsoft.AspNetCore.Mvc.ControllerBase";
        if (compilation.GetTypeByMetadataName(ControllerBaseInstance) is not { } controllerBaseInstance)
        {
            return false;
        }

        const string ApiControllerAttribute = "Microsoft.AspNetCore.Mvc.ApiControllerAttribute";
        if (compilation.GetTypeByMetadataName(ApiControllerAttribute) is not { } apiControllerAttribute)
        {
            return false;
        }

        const string ControllerAttribute = "Microsoft.AspNetCore.Mvc.ControllerAttribute";
        if (compilation.GetTypeByMetadataName(ControllerAttribute) is not { } controllerAttribute)
        {
            return false;
        }

        const string IActionFilter = "Microsoft.AspNetCore.Mvc.Filters.IActionFilter";
        if (compilation.GetTypeByMetadataName(IActionFilter) is not { } actionFilterInstance)
        {
            return false;
        }

        const string SignalRHub = "Microsoft.AspNetCore.SignalR.Hub";
        if (compilation.GetTypeByMetadataName(SignalRHub) is not { } signalRHub)
        {
            return false;
        }

        const string PageModel = "Microsoft.AspNetCore.Mvc.RazorPages.PageModel";
        if (compilation.GetTypeByMetadataName(PageModel) is not { } pageModel)
        {
            return false;
        }

        const string NonHandlerAttribute = "Microsoft.AspNetCore.Mvc.RazorPages.NonHandlerAttribute";
        if (compilation.GetTypeByMetadataName(NonHandlerAttribute) is not { } nonHandlerAttribute)
        {
            return false;
        }

        wellKnownTypes = new WellKnownTypes
        {
            ControllerBaseInstance = controllerBaseInstance,
            ApiControllerAttribute = apiControllerAttribute,
            ControllerAttribute = controllerAttribute,
            IActionFilter = actionFilterInstance,
            SignalRHub = signalRHub,
            PageModel = pageModel,
            NonHandlerAttribute = nonHandlerAttribute,
        };

        return true;
    }

    public INamedTypeSymbol ControllerBaseInstance { get; private init; }

    public INamedTypeSymbol ApiControllerAttribute { get; private init; }

    public INamedTypeSymbol ControllerAttribute { get; private init; }

    public INamedTypeSymbol IActionFilter { get; private init; }

    public INamedTypeSymbol SignalRHub { get; private init; }

    public INamedTypeSymbol PageModel { get; private init; }

    public INamedTypeSymbol NonHandlerAttribute { get; private init; }
}
