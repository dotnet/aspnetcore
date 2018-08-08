// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    internal readonly struct ApiControllerSymbolCache
    {
        public ApiControllerSymbolCache(Compilation compilation)
        {
            ApiConventionMethodAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ApiConventionMethodAttribute);
            ApiConventionNameMatchAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ApiConventionNameMatchAttribute);
            ApiConventionTypeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ApiConventionTypeAttribute);
            ApiConventionTypeMatchAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ApiConventionTypeMatchAttribute);
            ControllerAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ControllerAttribute);
            DefaultStatusCodeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.DefaultStatusCodeAttribute);
            IActionResult = compilation.GetTypeByMetadataName(ApiSymbolNames.IActionResult);
            IApiBehaviorMetadata = compilation.GetTypeByMetadataName(ApiSymbolNames.IApiBehaviorMetadata);
            ModelStateDictionary = compilation.GetTypeByMetadataName(ApiSymbolNames.ModelStateDictionary);
            NonActionAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.NonActionAttribute);
            NonControllerAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.NonControllerAttribute);
            ProducesDefaultResponseTypeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ProducesDefaultResponseTypeAttribute);
            ProducesResponseTypeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ProducesResponseTypeAttribute);

            StatusCodeValueAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.StatusCodeValueAttribute);

            var statusCodeActionResult = compilation.GetTypeByMetadataName(ApiSymbolNames.IStatusCodeActionResult);
            StatusCodeActionResultStatusProperty = (IPropertySymbol)statusCodeActionResult.GetMembers("StatusCode")[0];

            var disposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            var members = disposable.GetMembers(nameof(IDisposable.Dispose));
            IDisposableDispose = members.Length == 1 ? (IMethodSymbol)members[0] : null;
        }

        public INamedTypeSymbol ApiConventionMethodAttribute { get; }

        public INamedTypeSymbol ApiConventionNameMatchAttribute { get; }

        public INamedTypeSymbol ApiConventionTypeAttribute { get; }

        public INamedTypeSymbol ApiConventionTypeMatchAttribute { get; }

        public INamedTypeSymbol ControllerAttribute { get; }

        public INamedTypeSymbol DefaultStatusCodeAttribute { get; }

        public INamedTypeSymbol IActionResult { get; }

        public INamedTypeSymbol IApiBehaviorMetadata { get; }

        public IMethodSymbol IDisposableDispose { get; }

        public IPropertySymbol StatusCodeActionResultStatusProperty { get; }

        public ITypeSymbol ModelStateDictionary { get; }

        public INamedTypeSymbol NonActionAttribute { get; }

        public INamedTypeSymbol NonControllerAttribute { get; }

        public INamedTypeSymbol ProducesDefaultResponseTypeAttribute { get; }

        public INamedTypeSymbol ProducesResponseTypeAttribute { get; }

        public INamedTypeSymbol StatusCodeValueAttribute { get; }
    }
}
