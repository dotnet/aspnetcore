// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal readonly struct ApiControllerTypeCache
    {
        public ApiControllerTypeCache(Compilation compilation)
        {
            ApiConventionAttribute = compilation.GetTypeByMetadataName(SymbolNames.ApiConventionAttribute);
            ProducesResponseTypeAttribute = compilation.GetTypeByMetadataName(SymbolNames.ProducesResponseTypeAttribute);
        }

        public INamedTypeSymbol ApiConventionAttribute { get; }

        public INamedTypeSymbol ProducesResponseTypeAttribute { get; }
    }
}
