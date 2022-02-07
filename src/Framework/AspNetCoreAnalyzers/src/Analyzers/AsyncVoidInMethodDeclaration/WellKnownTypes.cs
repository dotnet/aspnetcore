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

        const string ControllerInstance = "Microsoft.AspNetCore.Mvc.Controller";
        if (compilation.GetTypeByMetadataName(ControllerInstance) is not { } controllerInstance)
        {
            return false;
        }

        const string IActionFilter = "Microsoft.AspNetCore.Mvc.Filters.IActionFilter";
        if (compilation.GetTypeByMetadataName(IActionFilter) is not { } actionFilterInstance)
        {
            return false;
        }

        wellKnownTypes = new WellKnownTypes
        {
            ControllerInstance = controllerInstance,
            IActionFilter = actionFilterInstance
        };

        return true;
    }

    public INamedTypeSymbol ControllerInstance { get; private init; }

    public INamedTypeSymbol IActionFilter { get; private init; }
}
