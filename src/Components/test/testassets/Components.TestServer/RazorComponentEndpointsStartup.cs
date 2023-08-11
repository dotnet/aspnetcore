// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using Components.TestServer.RazorComponents;
using Components.TestServer.RazorComponents.Pages.Forms;
using Components.TestServer.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Server;

namespace TestServer;

public class RazorComponentEndpointsStartup<TRootComponent>
{
    public RazorComponentEndpointsStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorComponents(options =>
        {
            options.MaxFormMappingErrorCount = 10;
            options.MaxFormMappingRecursionDepth = 5;
            options.MaxFormMappingCollectionSize = 100;
        })
            .AddWebAssemblyComponents()
            .AddServerComponents();
        services.AddHttpContextAccessor();
        services.AddSingleton<AsyncOperationService>();
        services.AddCascadingAuthenticationState();
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

        app.Map("/subdir", app =>
        {
            app.UseStaticFiles();
            app.UseRouting();
            UseFakeAuthState(app);
            app.UseAntiforgery();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorComponents<TRootComponent>()
                    .AddAdditionalAssemblies(Assembly.Load("Components.WasmMinimal"))
                    .AddServerRenderMode()
                    .AddWebAssemblyRenderMode(new WebAssemblyComponentsEndpointOptions
                    {
                        PathPrefix = "/WasmMinimal"
                    });

                NotEnabledStreamingRenderingComponent.MapEndpoints(endpoints);
                StreamingRenderingForm.MapEndpoints(endpoints);
                InteractiveStreamingRenderingComponent.MapEndpoints(endpoints);

                MapEnhancedNavigationEndpoints(endpoints);
            });
        });
    }

    private static void UseFakeAuthState(IApplicationBuilder app)
    {
        app.Use((HttpContext context, Func<Task> next) =>
        {
            // Completely insecure fake auth system with no password for tests. Do not do anything like this in real apps.
            // It accepts a query parameter 'username' and then sets or deletes a cookie to hold that, and supplies a principal
            // using this username (taken either from the cookie or query param).
            const string cookieKey = "fake_username";
            context.Request.Cookies.TryGetValue(cookieKey, out var username);
            if (context.Request.Query.TryGetValue("username", out var usernameFromQuery))
            {
                username = usernameFromQuery;
                if (string.IsNullOrEmpty(username))
                {
                    context.Response.Cookies.Delete(cookieKey);
                }
                else
                {
                    // Expires when browser is closed, so tests won't interfere with each other
                    context.Response.Cookies.Append(cookieKey, username);
                }
            }

            if (!string.IsNullOrEmpty(username))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim("test-claim", "Test claim value"),
                };

                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "FakeAuthenticationType"));
            }

            return next();
        });
    }

    private static void MapEnhancedNavigationEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Used when testing that enhanced nav can show non-HTML responses (which it does by doing a full navigation)
        endpoints.Map("/nav/non-html-response", () => "Hello, this is plain text");

        // Used when testing that enhanced nav displays content even if the response is an error status code
        endpoints.Map("/nav/give-404-with-content", async (HttpResponse response) =>
        {
            response.StatusCode = 404;
            response.ContentType = "text/html";
            await response.WriteAsync("<h1>404</h1><p>Sorry, there's nothing here! This is a custom server-generated 404 message.</p>");
        });
    }
}
