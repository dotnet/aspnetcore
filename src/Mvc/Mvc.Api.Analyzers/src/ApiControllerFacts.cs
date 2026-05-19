// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Shared;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

internal static class ApiControllerFacts
{
    public static bool IsApiControllerAction(ApiControllerSymbolCache symbolCache, IMethodSymbol method)
    {
        if (method == null)
        {
            return false;
        }

        if (method.ReturnsVoid || method.ReturnType.TypeKind == TypeKind.Error)
        {
            return false;
        }

        if (!MvcFacts.IsController(method.ContainingType, symbolCache.ControllerAttribute, symbolCache.NonControllerAttribute))
        {
            return false;
        }

        if (!method.ContainingType.HasAttribute(symbolCache.IApiBehaviorMetadata, inherit: true) &&
            !method.ContainingAssembly.HasAttribute(symbolCache.IApiBehaviorMetadata))
        {
            return false;
        }

        if (!MvcFacts.IsControllerAction(method, symbolCache.NonActionAttribute, symbolCache.IDisposableDispose))
        {
            return false;
        }

        return true;
    }
}
