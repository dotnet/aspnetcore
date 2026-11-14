// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TestServer;

public class RemoteMsalAuthenticationStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.Map("/subdir", app =>
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

                endpoints.MapRazorComponents<RemoteMsalAuthenticationApp>()
                    .AddAdditionalAssemblies(Assembly.Load("Components.WasmMsalRemoteAuthentication"))
                    .AddInteractiveWebAssemblyRenderMode(options => options.PathPrefix = "/WasmMsalRemoteAuthentication");

                var oidcEndpoints = endpoints.MapGroup("oidc");

                var issuer = "";
                oidcEndpoints.MapGet(".well-known/openid-configuration", (HttpRequest request, [FromHeader] string host) =>
                {
                    issuer = $"{(request.IsHttps ? "https" : "http")}://{host}";
                    return Results.Json(new
                    {
                        issuer,
                        authorization_endpoint = $"{issuer}/subdir/oidc/authorize",
                        token_endpoint = $"{issuer}/subdir/oidc/token",
                    });
                });

                var lastCode = "";
                var interactiveAuthorizationRequests = 0;
                oidcEndpoints.MapGet("authorize", (string redirect_uri, string? state, string? prompt) =>
                {
                    if (prompt == "none")
                    {
                        return Results.Redirect($"{redirect_uri}?error=interaction_required&state={state}");
                    }

                    interactiveAuthorizationRequests++;
                    lastCode = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
                    return Results.Redirect($"{redirect_uri}?code={lastCode}&state={state}");
                });

                oidcEndpoints.MapGet("interactive-authorization-requests", () => interactiveAuthorizationRequests);

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
            });
        });
    }
}
