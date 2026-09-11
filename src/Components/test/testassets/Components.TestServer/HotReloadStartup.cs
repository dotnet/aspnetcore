// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.WebSockets;
using System.Reflection;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Http.Features;

namespace TestServer;

public class HotReloadStartup
{
    public HotReloadStartup()
    {
        AppContext.SetSwitch("System.Reflection.Metadata.MetadataUpdater.IsSupported", true);

        // HotReloadManager captures the AppContext switch value once into a static field at static
        // initialization, so the AppContext.SetSwitch call above has no effect once another in-process
        // server has already initialized it. Force the cached field to true via reflection so hot reload
        // is active for this server. This is safe only because the E2E suite runs serially.
        typeof(HotReloadManager).GetField("s_isSupported", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, true);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddRazorPages();
        services.AddServerSideBlazor();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var enUs = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = enUs;
        CultureInfo.DefaultThreadCurrentUICulture = enUs;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapBlazorHub().AddEndpointFilter(async (context, next) =>
            {
                if (context.HttpContext.WebSockets.IsWebSocketRequest)
                {
                    var currentFeature = context.HttpContext.Features.Get<IHttpWebSocketFeature>(); context.HttpContext.Features.Set<IHttpWebSocketFeature>(new ServerComponentsSocketFeature(currentFeature!));
                }
                return await next(context);
            });
            endpoints.MapFallbackToPage("/_ServerHost");
        });
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
