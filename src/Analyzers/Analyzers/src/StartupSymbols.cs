// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class StartupSymbols
{
    public StartupSymbols(Compilation compilation)
    {
        IApplicationBuilder = compilation.GetBestTypeByMetadataName(SymbolNames.IApplicationBuilder.MetadataName);
        IServiceCollection = compilation.GetBestTypeByMetadataName(SymbolNames.IServiceCollection.MetadataName);
        MvcOptions = compilation.GetBestTypeByMetadataName(SymbolNames.MvcOptions.MetadataName);
    }

    public bool HasRequiredSymbols => IApplicationBuilder != null && IServiceCollection != null;

    public INamedTypeSymbol IApplicationBuilder { get; }

    public INamedTypeSymbol IServiceCollection { get; }

    public INamedTypeSymbol MvcOptions { get; }
}
