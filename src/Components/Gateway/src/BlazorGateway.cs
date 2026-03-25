// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

/// <summary>
/// Intended for framework test use only.
/// </summary>
public static class BlazorGateway
{
    /// <summary>
    /// Builds a <see cref="WebApplication"/> configured as a Blazor Gateway.
    /// Supports two modes:
    /// <list type="bullet">
    /// <item>Standalone: derives manifest paths from --applicationpath CLI arg</item>
    /// <item>Aspire: reads ClientApps config section, YARP reverse proxy, etc.</item>
    /// </list>
    /// </summary>
    public static WebApplication BuildWebHost(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Standalone mode: derive manifest paths from --applicationpath
        var applicationPath = args.SkipWhile(a => a != "--applicationpath").Skip(1).FirstOrDefault();
        if (applicationPath != null)
        {
            var runtimeManifest = Path.ChangeExtension(applicationPath, ".staticwebassets.runtime.json");
            builder.Configuration[WebHostDefaults.StaticWebAssetsKey] = runtimeManifest;

            var endpointsManifest = Path.ChangeExtension(applicationPath, ".staticwebassets.endpoints.json");
            builder.Configuration["ClientApps:app:EndpointsManifest"] = endpointsManifest;
            builder.Configuration["ClientApps:app:PathPrefix"] = "";
        }

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
                app.MapGroup(appConfig.PathPrefix ?? "").MapStaticAssets(appConfig.EndpointsManifest)
                    .Add(ep =>
                    {
                        if (ep is RouteEndpointBuilder reb && reb.RoutePattern.RawText?.Contains("{**path") == true)
                        {
                            reb.Order = int.MaxValue;
                        }
                    });
            }
        }

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
