// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TestServer;

public class RemoteAuthenticationAppStartup
{
    public const string TokenStorageBasePath = "/subdir-tokenstorage";
    public const string SessionStorageBasePath = "/subdir-session-storage";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.Map(TokenStorageBasePath, app => ConfigureApp(app, env, TokenStorageBasePath));
        app.Map(SessionStorageBasePath, app => ConfigureApp(app, env, SessionStorageBasePath));
    }

    private static void ConfigureApp(IApplicationBuilder app, IWebHostEnvironment env, string basePath)
    {
        app.UseRouting();
        app.UseAntiforgery();
        app.UseEndpoints(endpoints =>
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

            endpoints.MapRazorComponents<RemoteAuthenticationApp>()
                .AddAdditionalAssemblies(Assembly.Load("Components.WasmRemoteAuthentication"))
                .AddInteractiveWebAssemblyRenderMode(options => options.PathPrefix = "/WasmRemoteAuthentication");

            MapOidcEndpoints(endpoints, basePath);
        });
    }

    private static void MapOidcEndpoints(IEndpointRouteBuilder endpoints, string basePath)
    {
        var oidcEndpoints = endpoints.MapGroup("oidc");
        var issuer = "";
        var lastCode = "";

        oidcEndpoints.MapGet(".well-known/openid-configuration", (HttpRequest request, [FromHeader] string host) =>
        {
            issuer = $"{(request.IsHttps ? "https" : "http")}://{host}{basePath}";
            return Results.Json(new
            {
                issuer,
                authorization_endpoint = $"{issuer}/oidc/authorize",
                token_endpoint = $"{issuer}/oidc/token",
            });
        });

        oidcEndpoints.MapGet("authorize", (string redirect_uri, string? state, string? prompt, bool? preservedExtraQueryParams) =>
        {
            if (prompt == "none")
            {
                return Results.Redirect($"{redirect_uri}?error=interaction_required&state={state}");
            }
            if (preservedExtraQueryParams != true)
            {
                return Results.Redirect($"{redirect_uri}?error=invalid_request&error_description=extraQueryParams%20not%20preserved&state={state}");
            }
            lastCode = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
            return Results.Redirect($"{redirect_uri}?code={lastCode}&state={state}");
        });

        var jwtHandler = new JsonWebTokenHandler();
        oidcEndpoints.MapPost("token", ([FromForm] string code) =>
        {
            if (string.IsNullOrEmpty(lastCode) && code != lastCode)
            {
                return Results.BadRequest("Bad code");
            }
            return Results.Json(new
            {
                token_type = "Bearer",
                scope = "openid profile",
                expires_in = 3600,
                id_token = jwtHandler.CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = issuer,
                    Audience = "s6BhdRkqt3",
                    Claims = new Dictionary<string, object>
                    {
                        ["sub"] = "248289761001",
                        ["name"] = "Jane Doe",
                    },
                }),
            });
        }).DisableAntiforgery();
    }
}
