// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticAssets;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

internal static class ComponentWebAssemblyConventions
{
    private static readonly string? s_dotnetModifiableAssemblies = GetNonEmptyEnvironmentVariableValue("DOTNET_MODIFIABLE_ASSEMBLIES");
    private static readonly string? s_aspnetcoreBrowserTools = GetNonEmptyEnvironmentVariableValue("__ASPNETCORE_BROWSER_TOOLS");

    private static string? GetNonEmptyEnvironmentVariableValue(string name)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    internal static void AddBlazorWebAssemblyConventions(
        IReadOnlyList<StaticAssetDescriptor> descriptors,
        IWebHostEnvironment webHostEnvironment)
    {
        var headers = new List<StaticAssetResponseHeader>
        {
            new("Blazor-Environment", webHostEnvironment.EnvironmentName)
        };

        // DOTNET_MODIFIABLE_ASSEMBLIES is used by the runtime to initialize hot-reload specific environment variables and is configured
        // by the launching process (dotnet-watch / Visual Studio).
        // Always add the header if the environment variable is set, regardless of the kind of environment.
        if (s_dotnetModifiableAssemblies != null)
        {
            headers.Add(new("DOTNET-MODIFIABLE-ASSEMBLIES", s_dotnetModifiableAssemblies));
        }

        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        // Translate the _ASPNETCORE_BROWSER_TOOLS environment configured by the browser tools agent in to a HTTP response header.
        if (s_aspnetcoreBrowserTools != null)
        {
            headers.Add(new("ASPNETCORE-BROWSER-TOOLS", s_aspnetcoreBrowserTools));
        }

        for (var i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];
            if (descriptor.AssetPath.StartsWith("_framework/", StringComparison.OrdinalIgnoreCase))
            {
                descriptor.ResponseHeaders = [
                    ..descriptor.ResponseHeaders,
                    ..headers];
            }
        }
    }
}
