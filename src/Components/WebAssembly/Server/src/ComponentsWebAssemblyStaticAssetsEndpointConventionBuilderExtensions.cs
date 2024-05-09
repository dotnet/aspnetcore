// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

/// <summary>
/// Extension methods for <see cref="StaticAssetsEndpointConventionBuilder"/>.
/// </summary>
public static class ComponentsWebAssemblyStaticAssetsEndpointConventionBuilderExtensions
{
    private static readonly string? s_dotnetModifiableAssemblies = GetNonEmptyEnvironmentVariableValue("DOTNET_MODIFIABLE_ASSEMBLIES");
    private static readonly string? s_aspnetcoreBrowserTools = GetNonEmptyEnvironmentVariableValue("__ASPNETCORE_BROWSER_TOOLS");

    private static string? GetNonEmptyEnvironmentVariableValue(string name)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    /// <summary>
    /// Configures additional static web asset extensions logic for Blazor WebAssembly.
    /// </summary>
    /// <param name="builder"></param>
    internal static void AddBlazorWebAssemblyConventions(this StaticAssetsEndpointConventionBuilder builder)
    {
        builder.Add(endpoint =>
        {
            if (endpoint is RouteEndpointBuilder { RoutePattern.RawText: { } pattern } && pattern.Contains("/_framework/", StringComparison.OrdinalIgnoreCase) &&
            !pattern.Contains("/_framework/blazor.server.js", StringComparison.OrdinalIgnoreCase) && !pattern.Contains("/_framework/blazor.web.js", StringComparison.OrdinalIgnoreCase))
            {
                WrapEndpoint(endpoint);
            }
        });
    }

    private static void WrapEndpoint(EndpointBuilder endpoint)
    {
        var original = endpoint.RequestDelegate;
        if (original == null)
        {
            return;
        }

        for (var i = 0; i < endpoint.Metadata.Count; i++)
        {
            if (endpoint.Metadata[i] is WebAssemblyConventionsAppliedMetadata)
            {
                // Already applied
                return;
            }
        }

        endpoint.Metadata.Add(new WebAssemblyConventionsAppliedMetadata());

        // Note this mimics what UseBlazorFrameworkFiles does.
        // The goal is to remove all this logic and push it to the build. For example, we should not have
        // "Cache-Control" "no-cache" here as the build itself will add it.
        // Similarly, we shouldn't add the `DOTNET-MODIFIABLE-ASSEMBLIES` and `ASPNETCORE-BROWSER-TOOLS` headers here.
        // Those should be handled by the tooling, by hooking up on to the OnResponseStarting event and checking that the
        // endpoint is for the appropriate web assembly file. (Very likely this is only needed for the blazor.boot.json file)
        endpoint.RequestDelegate = (context) =>
        {
            var webHostEnvironment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            context.Response.Headers.Add("Blazor-Environment", webHostEnvironment.EnvironmentName);
            context.Response.Headers.Add("Cache-Control", "no-cache");

            // DOTNET_MODIFIABLE_ASSEMBLIES is used by the runtime to initialize hot-reload specific environment variables and is configured
            // by the launching process (dotnet-watch / Visual Studio).
            // Always add the header if the environment variable is set, regardless of the kind of environment.
            if (s_dotnetModifiableAssemblies != null)
            {
                context.Response.Headers.Add("DOTNET-MODIFIABLE-ASSEMBLIES", s_dotnetModifiableAssemblies);
            }

            // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
            // Translate the _ASPNETCORE_BROWSER_TOOLS environment configured by the browser tools agent in to a HTTP response header.
            if (s_aspnetcoreBrowserTools != null)
            {
                context.Response.Headers.Add("ASPNETCORE-BROWSER-TOOLS", s_aspnetcoreBrowserTools);
            }

            return original(context);
        };
    }

    private sealed class WebAssemblyConventionsAppliedMetadata { };
}
