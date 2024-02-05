// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace TestServer;

public class TransportsServerStartup : ServerStartup
{
    public TransportsServerStartup(IConfiguration configuration)
        : base(configuration)
    {
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog resourceRequestLog)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.Map("/defaultTransport", app =>
        {
            app.UseStaticFiles();

            app.UseRouting();
            app.UseWebSockets();
            app.UseEndpoints(endpoints =>
            {
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
                endpoints.MapFallbackToPage("/_ServerHost");
            });
        });

        app.Map("/longPolling", app =>
        {
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub(configureOptions: options =>
                {
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                });
                endpoints.MapFallbackToPage("/_ServerHost");
            });
        });

        app.Map("/webSockets", app =>
        {
            app.UseStaticFiles();

            app.UseRouting();
            app.UseWebSockets();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub(configureOptions: options =>
                {
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                }).AddEndpointFilter(async (context, next) =>
                    {
                        if (context.HttpContext.WebSockets.IsWebSocketRequest)
                        {
                            var currentFeature = context.HttpContext.Features.Get<IHttpWebSocketFeature>();

                            context.HttpContext.Features.Set<IHttpWebSocketFeature>(new ServerComponentsSocketFeature(currentFeature!));
                        }
                        return await next(context);
                    });
                endpoints.MapFallbackToPage("/_ServerHost");
            });
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
