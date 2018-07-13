// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal readonly struct ApiControllerSymbolCache
    {
        public ApiControllerSymbolCache(Compilation compilation)
        {
            ActionResultOfT = compilation.GetTypeByMetadataName(SymbolNames.ActionResultOfT);
            ApiConventionNameMatchAttribute = compilation.GetTypeByMetadataName(SymbolNames.ApiConventionNameMatchAttribute);
            ApiConventionTypeAttribute = compilation.GetTypeByMetadataName(SymbolNames.ApiConventionTypeAttribute);
            ApiConventionTypeMatchAttribute = compilation.GetTypeByMetadataName(SymbolNames.ApiConventionTypeMatchAttribute);
            ControllerAttribute = compilation.GetTypeByMetadataName(SymbolNames.ControllerAttribute);
            DefaultStatusCodeAttribute = compilation.GetTypeByMetadataName(SymbolNames.DefaultStatusCodeAttribute);
            IActionResult = compilation.GetTypeByMetadataName(SymbolNames.IActionResult);
            IApiBehaviorMetadata = compilation.GetTypeByMetadataName(SymbolNames.IApiBehaviorMetadata);
            IConvertToActionResult = compilation.GetTypeByMetadataName(SymbolNames.IConvertToActionResult);
            NonActionAttribute = compilation.GetTypeByMetadataName(SymbolNames.NonActionAttribute);
            NonControllerAttribute = compilation.GetTypeByMetadataName(SymbolNames.NonControllerAttribute);
            ProducesResponseTypeAttribute = compilation.GetTypeByMetadataName(SymbolNames.ProducesResponseTypeAttribute);

            var disposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            var members = disposable.GetMembers(nameof(IDisposable.Dispose));
            IDisposableDispose = members.Length == 1 ? (IMethodSymbol)members[0] : null;
        }

        public INamedTypeSymbol ActionResultOfT { get; }

        public INamedTypeSymbol ApiConventionNameMatchAttribute { get; }

        public INamedTypeSymbol ApiConventionTypeAttribute { get; }

        public INamedTypeSymbol ApiConventionTypeMatchAttribute { get; }

        public INamedTypeSymbol ControllerAttribute { get; }

        public INamedTypeSymbol DefaultStatusCodeAttribute { get; }

        public INamedTypeSymbol IActionResult { get; }

        public INamedTypeSymbol IApiBehaviorMetadata { get; }

        public INamedTypeSymbol IConvertToActionResult { get; }

        public IMethodSymbol IDisposableDispose { get; }

        public INamedTypeSymbol NonActionAttribute { get; }

        public INamedTypeSymbol NonControllerAttribute { get; }

        public INamedTypeSymbol ProducesResponseTypeAttribute { get; }
    }
}
