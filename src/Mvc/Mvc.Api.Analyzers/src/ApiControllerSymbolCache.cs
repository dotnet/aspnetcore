// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    internal readonly struct ApiControllerSymbolCache
    {
        private readonly IPropertySymbol? _statusCodeActionResultStatusProperty;
        private readonly IMethodSymbol? _iDisposableDispose;

        public ApiControllerSymbolCache(Compilation compilation)
        {
            HasRequiredSymbols = true;

            ApiConventionMethodAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ApiConventionMethodAttribute);
            HasRequiredSymbols &= IsValidSymbol(ApiConventionMethodAttribute);

            ApiConventionNameMatchAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ApiConventionNameMatchAttribute);
            HasRequiredSymbols &= IsValidSymbol(ApiConventionNameMatchAttribute);

            ApiConventionTypeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ApiConventionTypeAttribute);
            HasRequiredSymbols &= IsValidSymbol(ApiConventionTypeAttribute);

            ApiConventionTypeMatchAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ApiConventionTypeMatchAttribute);
            HasRequiredSymbols &= IsValidSymbol(ApiConventionTypeMatchAttribute);

            ControllerAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ControllerAttribute);
            HasRequiredSymbols &= IsValidSymbol(ControllerAttribute);

            DefaultStatusCodeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.DefaultStatusCodeAttribute);
            HasRequiredSymbols &= IsValidSymbol(DefaultStatusCodeAttribute);

            IActionResult = compilation.GetTypeByMetadataName(ApiSymbolNames.IActionResult);
            HasRequiredSymbols &= IsValidSymbol(IActionResult);

            IApiBehaviorMetadata = compilation.GetTypeByMetadataName(ApiSymbolNames.IApiBehaviorMetadata);
            HasRequiredSymbols &= IsValidSymbol(IApiBehaviorMetadata);

            ModelStateDictionary = compilation.GetTypeByMetadataName(ApiSymbolNames.ModelStateDictionary);
            HasRequiredSymbols &= IsValidSymbol(ModelStateDictionary);

            NonActionAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.NonActionAttribute);
            HasRequiredSymbols &= IsValidSymbol(NonActionAttribute);

            NonControllerAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.NonControllerAttribute);
            HasRequiredSymbols &= IsValidSymbol(NonControllerAttribute);

            ProblemDetails = compilation.GetTypeByMetadataName(ApiSymbolNames.ProblemDetails);
            HasRequiredSymbols &= IsValidSymbol(ProblemDetails);

            ProducesDefaultResponseTypeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ProducesDefaultResponseTypeAttribute);
            HasRequiredSymbols &= IsValidSymbol(ProducesDefaultResponseTypeAttribute);

            ProducesErrorResponseTypeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ProducesErrorResponseTypeAttribute);
            HasRequiredSymbols &= IsValidSymbol(ProducesErrorResponseTypeAttribute);

            ProducesResponseTypeAttribute = compilation.GetTypeByMetadataName(ApiSymbolNames.ProducesResponseTypeAttribute);
            HasRequiredSymbols &= IsValidSymbol(ProducesResponseTypeAttribute);

            var statusCodeActionResult = compilation.GetTypeByMetadataName(ApiSymbolNames.IStatusCodeActionResult);
            _statusCodeActionResultStatusProperty = (IPropertySymbol?)statusCodeActionResult?.GetMembers("StatusCode")[0];
            HasRequiredSymbols &= IsValidSymbol(_statusCodeActionResultStatusProperty);

            var disposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            var members = disposable?.GetMembers(nameof(IDisposable.Dispose));
            _iDisposableDispose = (IMethodSymbol?)members?[0];
            HasRequiredSymbols &= IsValidSymbol(_iDisposableDispose);

            static bool IsValidSymbol(ISymbol? symbol) => symbol != null && symbol.Kind != SymbolKind.ErrorType;
        }

        public INamedTypeSymbol ApiConventionMethodAttribute { get; }

        public INamedTypeSymbol ApiConventionNameMatchAttribute { get; }

        public INamedTypeSymbol ApiConventionTypeAttribute { get; }

        public INamedTypeSymbol ApiConventionTypeMatchAttribute { get; }

        public INamedTypeSymbol ControllerAttribute { get; }

        public INamedTypeSymbol DefaultStatusCodeAttribute { get; }

        public INamedTypeSymbol IActionResult { get; }

        public INamedTypeSymbol IApiBehaviorMetadata { get; }

        public IMethodSymbol IDisposableDispose => _iDisposableDispose ?? throw new ArgumentNullException(nameof(IDisposableDispose));

        public IPropertySymbol StatusCodeActionResultStatusProperty => _statusCodeActionResultStatusProperty ?? throw new ArgumentNullException(nameof(StatusCodeActionResultStatusProperty));

        public ITypeSymbol ModelStateDictionary { get; }

        public INamedTypeSymbol NonActionAttribute { get; }

        public INamedTypeSymbol NonControllerAttribute { get; }

        public INamedTypeSymbol ProblemDetails { get; }

        public INamedTypeSymbol ProducesDefaultResponseTypeAttribute { get; }

        public INamedTypeSymbol ProducesResponseTypeAttribute { get; }

        public INamedTypeSymbol ProducesErrorResponseTypeAttribute { get; }

        public bool HasRequiredSymbols { get; }
    }
}
