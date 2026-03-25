// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

var pathBase = builder.Configuration.GetValue<string>("pathbase");

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

app.Run();

sealed class ClientAppConfiguration
{
    public string? PathPrefix { get; set; }
    public string? EndpointsManifest { get; set; }
    public string? ConfigEndpointPath { get; set; }
    public string? ConfigResponse { get; set; }
}
