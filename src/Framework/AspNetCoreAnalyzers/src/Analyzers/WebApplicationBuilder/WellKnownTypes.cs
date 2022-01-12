// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

internal sealed class WellKnownTypes
{
    public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = default;

        const string ConfigureHostBuilder = "Microsoft.AspNetCore.Builder.ConfigureHostBuilder";
        if (compilation.GetTypeByMetadataName(ConfigureHostBuilder) is not { } configureHostBuilder)
        {
            return false;
        }

        const string ConfigureWebHostBuilder = "Microsoft.AspNetCore.Builder.ConfigureWebHostBuilder";
        if (compilation.GetTypeByMetadataName(ConfigureWebHostBuilder) is not { } configureWebHostBuilder)
        {
            return false;
        }

        const string GenericHostWebHostBuilderExtensions = "Microsoft.Extensions.Hosting.GenericHostWebHostBuilderExtensions";
        if (compilation.GetTypeByMetadataName(GenericHostWebHostBuilderExtensions) is not { } genericHostWebHostBuilderExtensions)
        {
            return false;
        }

        const string WebHostBuilderExtensions = "Microsoft.AspNetCore.Hosting.WebHostBuilderExtensions";
        if (compilation.GetTypeByMetadataName(WebHostBuilderExtensions) is not { } webHostBuilderExtensions)
        {
            return false;
        }

        const string HostingAbstractionsWebHostBuilderExtensions = "Microsoft.AspNetCore.Hosting.HostingAbstractionsWebHostBuilderExtensions";
        if (compilation.GetTypeByMetadataName(HostingAbstractionsWebHostBuilderExtensions) is not { } hostingAbstractionsWebHostBuilderExtensions)
        {
            return false;
        }

        wellKnownTypes = new WellKnownTypes
        {
            ConfigureHostBuilder = configureHostBuilder,
            ConfigureWebHostBuilder = configureWebHostBuilder,
            GenericHostWebHostBuilderExtensions = genericHostWebHostBuilderExtensions,
            HostingAbstractionsWebHostBuilderExtensions = hostingAbstractionsWebHostBuilderExtensions,
            WebHostBuilderExtensions = webHostBuilderExtensions,
        };

        return true;
    }

    public INamedTypeSymbol ConfigureHostBuilder { get; private init; }
    public INamedTypeSymbol ConfigureWebHostBuilder { get; private init; }
    public INamedTypeSymbol GenericHostWebHostBuilderExtensions { get; private init; }
    public INamedTypeSymbol HostingAbstractionsWebHostBuilderExtensions { get; private init; }
    public INamedTypeSymbol WebHostBuilderExtensions { get; private init; }
}
