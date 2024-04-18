// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.WebSockets;
using BasicTestApp;
using Microsoft.AspNetCore.Http.Features;

namespace TestServer;

public class SaveState
{
    public SaveState(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc();
        services.AddServerSideBlazor();
        services.AddScoped<PreserveStateService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
            endpoints.MapRazorPages();
            endpoints.MapBlazorHub()
                .AddEndpointFilter(async (context, next) =>
                {
                    if (context.HttpContext.WebSockets.IsWebSocketRequest)
                    {
                        var currentFeature = context.HttpContext.Features.Get<IHttpWebSocketFeature>();

                        context.HttpContext.Features.Set<IHttpWebSocketFeature>(new ServerComponentsSocketFeature(currentFeature!));
                    }
                    return await next(context);
                });

            endpoints.MapFallbackToPage("/SaveState");
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
