// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class StartupSymbols
    {
        public StartupSymbols(Compilation compilation)
        {
            IApplicationBuilder = compilation.GetTypeByMetadataName(SymbolNames.IApplicationBuilder.MetadataName);
            IServiceCollection = compilation.GetTypeByMetadataName(SymbolNames.IServiceCollection.MetadataName);
            MvcOptions = compilation.GetTypeByMetadataName(SymbolNames.MvcOptions.MetadataName);
        }

        public bool HasRequiredSymbols => IApplicationBuilder != null && IServiceCollection != null;

        public INamedTypeSymbol IApplicationBuilder { get; }

        public INamedTypeSymbol IServiceCollection { get; }

        public INamedTypeSymbol MvcOptions { get; }
    }
}
