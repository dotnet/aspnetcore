// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class StartupSymbols
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
