// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Intended for framework test use only.
/// </summary>
public static class BlazorGateway
{
    /// <summary>
    /// Builds a <see cref="WebApplication"/> configured as a Blazor Gateway.
    /// Reads ClientApps config section for endpoint manifests and YARP reverse proxy configuration.
    /// </summary>
    public static WebApplication BuildWebHost(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseStaticWebAssets();

        var appConfigs = builder.Configuration.GetSection("ClientApps")
            .Get<Dictionary<string, ClientAppConfiguration>>() ?? [];

        var proxySection = builder.Configuration.GetSection("ReverseProxy");
        var hasProxy = proxySection.Exists();

        if (hasProxy)
        {
            builder.Services.AddReverseProxy()
                .LoadFromConfig(proxySection)
                .AddServiceDiscoveryDestinationResolver();
        }

        var app = builder.Build();

        var pathBase = builder.Configuration.GetValue<string>("pathbase");
        if (!string.IsNullOrEmpty(pathBase))
        {
            app.UsePathBase(pathBase);
        }

        if (hasProxy)
        {
            app.MapReverseProxy();
        }

        foreach (var appConfig in appConfigs.Values)
        {
            if (!string.IsNullOrEmpty(appConfig.ConfigEndpointPath) && !string.IsNullOrEmpty(appConfig.ConfigResponse))
            {
                app.MapGet(appConfig.ConfigEndpointPath, () => Results.Content(appConfig.ConfigResponse, "application/json"))
                    .WithMetadata(new ContentEncodingMetadata("identity", 1.0));
            }

            if (!string.IsNullOrEmpty(appConfig.EndpointsManifest))
            {
                app.MapGroup(appConfig.PathPrefix ?? "").MapStaticAssets(appConfig.EndpointsManifest);
            }
        }

        // SPA fallback: serve index.html for any non-file URL path, similar to
        // the old DevServer's MapFallbackToFile("index.html"). This is registered
        // after MapStaticAssets so that specific static file routes take precedence.
        app.MapFallbackToFile("index.html");

        return app;
    }
}

sealed class ClientAppConfiguration
{
    public string? PathPrefix { get; set; }
    public string? EndpointsManifest { get; set; }
    public string? ConfigEndpointPath { get; set; }
    public string? ConfigResponse { get; set; }
}
