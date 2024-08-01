// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Web;
using Components.TestServer.RazorComponents;
using Components.TestServer.RazorComponents.Pages.Forms;
using Components.TestServer.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.AspNetCore.Mvc;

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
            .AddInteractiveWebAssemblyComponents()
            .AddInteractiveServerComponents()
            .AddAuthenticationStateSerialization(options =>
            {
                bool.TryParse(Configuration["SerializeAllClaims"], out var serializeAllClaims);
                options.SerializeAllClaims = serializeAllClaims;
            });

        services.AddHttpContextAccessor();
        services.AddSingleton<AsyncOperationService>();
        services.AddCascadingAuthenticationState();
        services.AddSingleton<WebSocketCompressionConfiguration>();

        var circuitContextAccessor = new TestCircuitContextAccessor();
        services.AddSingleton<CircuitHandler>(circuitContextAccessor);
        services.AddSingleton(circuitContextAccessor);
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
            WebAssemblyTestHelper.ServeCoopHeadersIfWebAssemblyThreadingEnabled(app);

            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
            }

            app.UseRouting();
            UseFakeAuthState(app);
            app.UseAntiforgery();

            app.Use((ctx, nxt) =>
            {
                if (ctx.Request.Query.ContainsKey("add-csp"))
                {
                    ctx.Response.Headers.Add("Content-Security-Policy", "script-src 'self' 'unsafe-inline'");
                }
                return nxt();
            });

            _ = app.UseEndpoints(endpoints =>
            {
                var contentRootStaticAssetsPath = Path.Combine(env.ContentRootPath, "Components.TestServer.staticwebassets.endpoints.json");
                if (File.Exists(contentRootStaticAssetsPath))
                {
                    endpoints.MapStaticAssets(contentRootStaticAssetsPath);
                }
                else
                {
                    endpoints.MapStaticAssets();
                }

                _ = endpoints.MapRazorComponents<TRootComponent>()
                    .AddAdditionalAssemblies(Assembly.Load("Components.WasmMinimal"))
                    .AddInteractiveServerRenderMode(options =>
                    {
                        var config = app.ApplicationServices.GetRequiredService<WebSocketCompressionConfiguration>();
                        options.DisableWebSocketCompression = config.IsCompressionDisabled;

                        options.ContentSecurityFrameAncestorsPolicy = config.CspPolicy;

                        options.ConfigureWebSocketAcceptContext = config.ConfigureWebSocketAcceptContext;
                    })
                    .AddInteractiveWebAssemblyRenderMode(options => options.PathPrefix = "/WasmMinimal");

                NotEnabledStreamingRenderingComponent.MapEndpoints(endpoints);
                StreamingRenderingForm.MapEndpoints(endpoints);
                InteractiveStreamingRenderingComponent.MapEndpoints(endpoints);

                MapEnhancedNavigationEndpoints(endpoints);
            });
        });
    }

    internal static void UseFakeAuthState(IApplicationBuilder app)
    {
        app.Use((HttpContext context, Func<Task> next) =>
        {
            // Completely insecure fake auth system with no password for tests. Do not do anything like this in real apps.
            // It accepts a query parameter 'username' and then sets or deletes a cookie to hold that, and supplies a principal
            // using this username (taken either from the cookie or query param).
            string GetQueryOrDefault(string queryKey, string defaultValue) =>
                context.Request.Query.TryGetValue(queryKey, out var value) ? value : defaultValue;

            const string cookieKey = "fake_username";
            var username = GetQueryOrDefault("username", context.Request.Cookies[cookieKey]);

            if (context.Request.Query.ContainsKey("username"))
            {
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

            var nameClaimType = GetQueryOrDefault("nameClaimType", ClaimTypes.Name);
            var roleClaimType = GetQueryOrDefault("roleClaimType", ClaimTypes.Role);

            if (!string.IsNullOrEmpty(username))
            {
                var claims = new List<Claim>
                {
                    new Claim(nameClaimType, username),
                    new Claim(roleClaimType, "test-role-1"),
                    new Claim(roleClaimType, "test-role-2"),
                    new Claim("test-claim", "Test claim value"),
                };

                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "FakeAuthenticationType", nameClaimType, roleClaimType));
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

        // Used when testing that enhanced nav includes "Accept: text/html"
        endpoints.Map("/nav/list-headers", async (HttpRequest request, HttpResponse response) =>
        {
            // We have to accept enanced nav explicitly since the test is checking what headers are sent for enhanced nav requests
            // Otherwise, the client will retry as a non-enhanced-nav request and the UI won't show the enhanced nav headers
            response.Headers.Add("blazor-enhanced-nav", "allow");

            response.ContentType = "text/html";
            await response.WriteAsync("<ul id='all-headers'>");
            foreach (var header in request.Headers)
            {
                await response.WriteAsync($"<li>{HttpUtility.HtmlEncode(header.Key)}: {HttpUtility.HtmlEncode(header.Value)}</li>");
            }
            await response.WriteAsync("</ul>");
        });

        // Used in the redirection to non-Blazor endpoints tests
        endpoints.MapGet("redirect/nonblazor/get", PerformRedirection);
        endpoints.MapPost("redirect/nonblazor/post", PerformRedirection);

        // Used when testing enhanced navigation to non-Blazor endpoints
        endpoints.Map("/nav/non-blazor-html-response", async (HttpResponse response) =>
        {
            response.ContentType = "text/html";
            await response.WriteAsync("<html><body><h1>This is a non-Blazor endpoint</h1><p>That's all</p></body></html>");
        });

        endpoints.MapPost("api/antiforgery-form", (
            [FromForm] string value,
            [FromForm(Name = "__RequestVerificationToken")] string? inFormCsrfToken,
            [FromHeader(Name = "RequestVerificationToken")] string? inHeaderCsrfToken) =>
        {
            // We shouldn't get this far without a valid CSRF token, but we'll double check it's there.
            if (string.IsNullOrEmpty(inFormCsrfToken) && string.IsNullOrEmpty(inHeaderCsrfToken))
            {
                throw new InvalidOperationException("Invalid POST to api/antiforgery-form!");
            }

            return TypedResults.Text($"<p id='pass'>Hello {value}!</p>", "text/html");
        });

        endpoints.Map("/forms/endpoint-that-never-finishes-rendering", (HttpResponse response, CancellationToken token) =>
        {
            return Task.Delay(Timeout.Infinite, token);
        });

        endpoints.Map("/test-formaction", () => "Formaction url");

        static Task PerformRedirection(HttpRequest request, HttpResponse response)
        {
            response.Redirect(request.Query["external"] == "true"
                ? "https://microsoft.com"
                : $"{request.PathBase}/nav/scroll-to-hash#some-content");
            return Task.CompletedTask;
        }
    }
}
