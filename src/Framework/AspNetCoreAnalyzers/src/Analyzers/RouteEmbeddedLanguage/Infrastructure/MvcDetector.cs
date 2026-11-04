// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Shared;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

using WellKnownType = WellKnownTypeData.WellKnownType;

internal static class MvcDetector
{
    public static bool IsController(INamedTypeSymbol? typeSymbol, WellKnownTypes wellKnownTypes)
    {
        if (typeSymbol is null)
        {
            return false;
        }

        return MvcFacts.IsController(
            typeSymbol,
            wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_ControllerAttribute),
            wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_NonControllerAttribute));
    }

    public static bool IsAction(IMethodSymbol methodSymbol, WellKnownTypes wellKnownTypes)
    {
        var disposable = wellKnownTypes.Get(SpecialType.System_IDisposable);
        var members = disposable.GetMembers(nameof(IDisposable.Dispose));
        var idisposableDispose = (IMethodSymbol)members[0];

        return MvcFacts.IsControllerAction(
            methodSymbol,
            wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_NonActionAttribute),
            idisposableDispose);
    }
}
