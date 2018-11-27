// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Analyzers;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
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
}
