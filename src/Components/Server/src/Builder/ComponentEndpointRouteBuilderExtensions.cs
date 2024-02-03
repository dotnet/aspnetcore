// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class ComponentEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Blazor <see cref="Hub" /> to the default path.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
    public static ComponentEndpointConventionBuilder MapBlazorHub(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapBlazorHub(ComponentHub.DefaultPath);
    }

    /// <summary>
    /// Maps the Blazor <see cref="Hub" /> to the path <paramref name="path"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="path">The path to map the Blazor <see cref="Hub" />.</param>
    /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
    public static ComponentEndpointConventionBuilder MapBlazorHub(
        this IEndpointRouteBuilder endpoints,
        string path)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(path);

        return endpoints.MapBlazorHub(path, configureOptions: _ => { });
    }

    /// <summary>
    ///Maps the Blazor <see cref="Hub" /> to the default path.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="configureOptions">A callback to configure dispatcher options.</param>
    /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
    public static ComponentEndpointConventionBuilder MapBlazorHub(
        this IEndpointRouteBuilder endpoints,
        Action<HttpConnectionDispatcherOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return endpoints.MapBlazorHub(ComponentHub.DefaultPath, configureOptions);
    }

    /// <summary>
    /// Maps the Blazor <see cref="Hub" /> to the path <paramref name="path"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="path">The path to map the Blazor <see cref="Hub" />.</param>
    /// <param name="configureOptions">A callback to configure dispatcher options.</param>
    /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
    public static ComponentEndpointConventionBuilder MapBlazorHub(
        this IEndpointRouteBuilder endpoints,
        string path,
        Action<HttpConnectionDispatcherOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var hubEndpoint = endpoints.MapHub<ComponentHub>(path, configureOptions)
            .AddEndpointFilter(async (context, next) =>
            {
                if (context.HttpContext.WebSockets.IsWebSocketRequest)
                {
                    var currentFeature = context.HttpContext.Features.Get<IHttpWebSocketFeature>();

                    context.HttpContext.Features.Set<IHttpWebSocketFeature>(new ServerComponentsSocketFeature(currentFeature!));
                }
                return await next(context);
            });

        hubEndpoint.Add(c =>
        {
            var originalDelegate = c.RequestDelegate;
            var builder = endpoints.CreateApplicationBuilder();
            builder.UseWebSockets();
            builder.Run(originalDelegate);
            c.RequestDelegate = builder.Build();
        });

        var disconnectEndpoint = endpoints.Map(
            (path.EndsWith('/') ? path : path + "/") + "disconnect/",
            endpoints.CreateApplicationBuilder().UseMiddleware<CircuitDisconnectMiddleware>().Build())
            .WithDisplayName("Blazor disconnect");

        var jsInitializersEndpoint = endpoints.Map(
            (path.EndsWith('/') ? path : path + "/") + "initializers/",
            endpoints.CreateApplicationBuilder().UseMiddleware<CircuitJavaScriptInitializationMiddleware>().Build())
            .WithDisplayName("Blazor initializers");

        var blazorEndpoint = GetBlazorEndpoint(endpoints);

        return new ComponentEndpointConventionBuilder(hubEndpoint, disconnectEndpoint, jsInitializersEndpoint, blazorEndpoint);
    }

    private static IEndpointConventionBuilder GetBlazorEndpoint(IEndpointRouteBuilder endpoints)
    {
        var options = new StaticFileOptions
        {
            FileProvider = new ManifestEmbeddedFileProvider(typeof(ComponentEndpointRouteBuilderExtensions).Assembly),
            OnPrepareResponse = CacheHeaderSettings.SetCacheHeaders
        };

        var app = endpoints.CreateApplicationBuilder();
        app.Use(next => context =>
        {
            // Set endpoint to null so the static files middleware will handle the request.
            context.SetEndpoint(null);

            return next(context);
        });
        app.UseStaticFiles(options);

        var blazorEndpoint = endpoints.Map("/_framework/blazor.server.js", app.Build())
            .WithDisplayName("Blazor static files");

        blazorEndpoint.Add((builder) => ((RouteEndpointBuilder)builder).Order = int.MinValue);

#if DEBUG
        // We only need to serve the sourcemap when working on the framework, not in the distributed packages
        endpoints.Map("/_framework/blazor.server.js.map", app.Build())
            .WithDisplayName("Blazor static files sourcemap")
            .Add((builder) => ((RouteEndpointBuilder)builder).Order = int.MinValue);
#endif

        return blazorEndpoint;
    }

    private sealed class ServerComponentsSocketFeature(IHttpWebSocketFeature originalFeature) : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => originalFeature.IsWebSocketRequest;

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            context.DangerousEnableCompression = true;
            return originalFeature.AcceptAsync(context);
        }
    }
}
