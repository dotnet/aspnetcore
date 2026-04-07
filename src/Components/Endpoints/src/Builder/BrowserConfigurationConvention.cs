// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Convention that ensures every Razor component endpoint has a <see cref="BrowserConfiguration"/>
/// in its metadata, populated with framework defaults.
/// </summary>
/// <remarks>
/// Registered as a <c>Finally</c> convention so it runs after user conventions
/// (e.g., <see cref="RazorComponentsEndpointConventionBuilderExtensions.WithBrowserConfiguration"/>).
/// This follows the same pattern as <c>ResourceCollectionConvention</c> for <c>ResourceAssetCollection</c>.
/// </remarks>
internal sealed class BrowserConfigurationConvention
{
    private static readonly string? s_dotnetModifiableAssemblies =
        Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES") is { Length: > 0 } v1 ? v1 : null;

    private static readonly string? s_aspnetcoreBrowserTools =
        Environment.GetEnvironmentVariable("__ASPNETCORE_BROWSER_TOOLS") is { Length: > 0 } v2 ? v2 : null;

    private readonly IHostEnvironment _hostEnvironment;

    public BrowserConfigurationConvention(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>
    /// Applies the convention to an endpoint builder. Ensures a <see cref="BrowserConfiguration"/>
    /// exists in metadata and merges framework defaults into it.
    /// </summary>
    public void ApplyConvention(EndpointBuilder endpointBuilder)
    {
        // Check if the user already added a BrowserConfiguration via WithBrowserConfiguration
        var existing = endpointBuilder.Metadata.OfType<BrowserConfiguration>().LastOrDefault();
        if (existing == null)
        {
            // No user configuration — add a default one
            existing = new BrowserConfiguration();
            endpointBuilder.Metadata.Add(existing);
        }

        // Apply framework defaults that the user didn't set
        existing.WebAssembly.EnvironmentName ??= _hostEnvironment.EnvironmentName;

        // Forward tooling environment variables so the WASM runtime can enable
        // hot-reload (DOTNET_MODIFIABLE_ASSEMBLIES) and browser link (__ASPNETCORE_BROWSER_TOOLS).
        if (s_dotnetModifiableAssemblies != null)
        {
            existing.WebAssembly.EnvironmentVariables.TryAdd("DOTNET_MODIFIABLE_ASSEMBLIES", s_dotnetModifiableAssemblies);
        }

        if (s_aspnetcoreBrowserTools != null)
        {
            existing.WebAssembly.EnvironmentVariables.TryAdd("__ASPNETCORE_BROWSER_TOOLS", s_aspnetcoreBrowserTools);
        }
    }
}
