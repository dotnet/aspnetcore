// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TestServer;

public class RemoteAuthenticationStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

                endpoints.MapRazorComponents<RemoteAuthenticationApp>()
                    .AddAdditionalAssemblies(Assembly.Load("Components.WasmRemoteAuthentication"))
                    .AddInteractiveWebAssemblyRenderMode(options => options.PathPrefix = "/WasmRemoteAuthentication");

                var oidcEndpoints = endpoints.MapGroup("oidc");

                // This is designed to test a single login at a time.
                var issuer = "";
                oidcEndpoints.MapGet(".well-known/openid-configuration", (HttpRequest request, [FromHeader] string host) =>
                {
                    issuer = $"{(request.IsHttps ? "https" : "http")}://{host}";
                    return Results.Json(new
                    {
                        issuer,
                        authorization_endpoint = $"{issuer}/subdir/oidc/authorize",
                        token_endpoint = $"{issuer}/subdir/oidc/token",
                        end_session_endpoint = $"{issuer}/subdir/oidc/logout",
                    });
                });

                var lastCode = "";
                oidcEndpoints.MapGet("authorize", (
                    string redirect_uri,
                    string? state,
                    string? prompt,
                    string? response_mode,
                    bool? preservedExtraQueryParams,
                    string? callbackResponseMode,
                    string? callbackError,
                    string? callbackErrorDescription) =>
                {
                    // The client declares where it expects the callback parameters.
                    var delimiter = string.Equals(response_mode, "fragment", StringComparison.Ordinal) ? "#" : "?";

                    // Require interaction so silent sign-in does not skip RedirectToLogin.razor.
                    if (prompt == "none")
                    {
                        return Results.Redirect($"{redirect_uri}{delimiter}error=interaction_required&state={state}");
                    }

                    // Verify that the extra query parameters added by RedirectToLogin.razor are preserved.
                    if (preservedExtraQueryParams != true)
                    {
                        return Results.Redirect($"{redirect_uri}{delimiter}error=invalid_request&error_description=extraQueryParams%20not%20preserved&state={state}");
                    }

                    if (!string.IsNullOrEmpty(callbackResponseMode))
                    {
                        callbackError ??= "access_denied";
                        var error = $"error={Uri.EscapeDataString(callbackError)}";
                        if (!string.IsNullOrEmpty(callbackErrorDescription))
                        {
                            error += $"&error_description={Uri.EscapeDataString(callbackErrorDescription)}";
                        }

                        var escapedState = Uri.EscapeDataString(state ?? string.Empty);

                        // 'query' is where the callback parameters belong for the authorization code
                        // flow client, 'fragment' for the client configured with response_mode=fragment.
                        if (string.Equals(callbackResponseMode, "query", StringComparison.Ordinal))
                        {
                            return Results.Redirect($"{redirect_uri}?{error}&state={escapedState}");
                        }

                        if (string.Equals(callbackResponseMode, "fragment", StringComparison.Ordinal))
                        {
                            return Results.Redirect($"{redirect_uri}#{error}&state={escapedState}");
                        }

                        // Emits a well-formed query callback while also placing an unrelated error in
                        // the fragment. A code-flow client must ignore the fragment entirely.
                        if (string.Equals(callbackResponseMode, "strayFragment", StringComparison.Ordinal))
                        {
                            return Results.Redirect($"{redirect_uri}?state={escapedState}#{error}");
                        }

                        return Results.BadRequest($"Unsupported callbackResponseMode '{callbackResponseMode}'.");
                    }

                    lastCode = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
                    return Results.Redirect($"{redirect_uri}{delimiter}code={lastCode}&state={state}");
                });

                oidcEndpoints.MapGet("logout", (string post_logout_redirect_uri, string? state) =>
                {
                    // The logout state is returned in the query string regardless of the response
                    // mode configured for sign-in, as oidc-client expects.
                    var separator = post_logout_redirect_uri.Contains('?') ? "&" : "?";
                    return Results.Redirect($"{post_logout_redirect_uri}{separator}state={Uri.EscapeDataString(state ?? string.Empty)}");
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
            });
        });
    }
}
