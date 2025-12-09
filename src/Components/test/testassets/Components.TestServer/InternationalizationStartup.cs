// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Localization;

namespace TestServer;

public class InternationalizationStartup
{
    public InternationalizationStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc();
        services.AddServerSideBlazor();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Mount the server-side Blazor app on /subdir
        app.Map("/subdir", app =>
        {
            WebAssemblyTestHelper.ServeCoopHeadersIfWebAssemblyThreadingEnabled(app);
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRequestLocalization(options =>
            {
                options.AddSupportedCultures("en-US", "fr-FR");
                options.AddSupportedUICultures("en-US", "fr-FR");

                // Cookie culture provider is included by default, but we want it to be the only one.
                options.RequestCultureProviders.Clear();
                options.RequestCultureProviders.Add(new CookieRequestCultureProvider());

                // We want the default to be en-US so that the tests for bind can work consistently.
                options.SetDefaultCulture("en-US");
            });

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
