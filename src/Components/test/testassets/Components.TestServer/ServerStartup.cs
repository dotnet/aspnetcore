// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;

namespace TestServer;

public class ServerStartup
{
    public ServerStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc();
        services.AddServerSideBlazor(options =>
        {
            options.RootComponents.MaxJSRootComponents = 5; // To make it easier to test
            options.RootComponents.RegisterForJavaScript<BasicTestApp.DynamicallyAddedRootComponent>("my-dynamic-root-component");
            options.RootComponents.RegisterForJavaScript<BasicTestApp.JavaScriptRootComponentParameterTypes>(
                "component-with-many-parameters",
                javaScriptInitializer: "myJsRootComponentInitializers.testInitializer");
        });
        services.AddSingleton<ResourceRequestLog>();
        services.AddTransient<BasicTestApp.FormsTest.ValidationComponentDI.SaladChef>();

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        services.AddValidation(options =>
            options.Resolvers.Add(new BasicTestApp.FormsTest.AsyncValidationResolver()));
#pragma warning restore ASP0029

        // Interactive host registers the gate so the async validation E2E tests control settling deterministically.
        services.AddSingleton<BasicTestApp.FormsTest.AsyncValidationGate>();

        var circuitContextAccessor = new TestCircuitContextAccessor();
        services.AddSingleton<CircuitHandler>(circuitContextAccessor);
        services.AddSingleton(circuitContextAccessor);

        services.AddKeyedSingleton(
            "keyed-service-1",
            BasicTestApp.PropertyInjection.TestKeyedService.Create("value-1"));
        services.AddKeyedSingleton(
            BasicTestApp.PropertyInjection.TestServiceKey.ServiceB,
            BasicTestApp.PropertyInjection.TestKeyedService.Create("value-2"));

        // Since tests run in parallel, we use an ephemeral key provider to avoid filesystem
        // contention issues.
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog resourceRequestLog)
    {
        var enUs = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = enUs;
        CultureInfo.DefaultThreadCurrentUICulture = enUs;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Mount the server-side Blazor app on /subdir
        app.Map("/subdir", app =>
        {
            app.Use((context, next) =>
            {
                if (context.Request.Path.Value.EndsWith("/images/blazor_logo_1000x.png", StringComparison.Ordinal))
                {
                    resourceRequestLog.AddRequest(context.Request);
                }

                // Opt-in strict style-src CSP for tests asserting CSP compliance.
                if (context.Request.Query.ContainsKey("strict-style-csp"))
                {
                    context.Response.Headers["Content-Security-Policy"] = "style-src 'self'";
                }

                return next(context);
            });

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
                endpoints.MapControllerRoute("mvc", "{controller}/{action}");
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
