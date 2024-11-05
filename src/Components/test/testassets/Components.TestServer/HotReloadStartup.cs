// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Http.Features;

namespace TestServer;

public class HotReloadStartup
{
    public HotReloadStartup()
    {
        HotReloadManager.Default.MetadataUpdateSupported = true;
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

        WebAssemblyTestHelper.ServeCoopHeadersIfWebAssemblyThreadingEnabled(app);
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
