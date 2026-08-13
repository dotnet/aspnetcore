// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionRevocationTests
{
    private const string SourceScheme = "Source";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string RefreshCookieName = ".AspNetCore.Source.Dbsc.Refresh";
    private const string RegistrationPath = "/.well-known/dbsc/registration";
    private const string RefreshPath = "/.well-known/dbsc/refresh";

    [Fact]
    public async Task Refresh_IsRejected_WhenSourceDelegateValidatePrincipalRevokes()
    {
        var revoked = false;
        using var host = await CreateHostAsync(configureSource: o =>
        {
            o.Events.OnValidatePrincipal = context =>
            {
                if (revoked)
                {
                    context.RejectPrincipal();
                }
                return Task.CompletedTask;
            };
        });
        var client = host.GetTestServer().CreateClient();

        var (refreshCookie, sessionId) = await SignInAndRegisterAsync(client);

        // While valid, the first leg of a refresh returns a 403 challenge.
        var beforeRevoke = await SendRefreshAsync(client, refreshCookie, sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, beforeRevoke.StatusCode);

        // After revocation, the source scheme's OnValidatePrincipal (forwarded onto the refresh cookie)
        // rejects the principal, so the refresh is unauthorized and the session terminates.
        revoked = true;
        var afterRevoke = await SendRefreshAsync(client, refreshCookie, sessionId);
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    [Fact]
    public async Task Refresh_IsRejected_WhenSourceEventsTypeValidatePrincipalRevokes()
    {
        using var host = await CreateHostAsync(
            configureSource: o => o.EventsType = typeof(RevokingEvents),
            configureServices: services =>
            {
                services.AddSingleton<RevocationState>();
                services.AddScoped<RevokingEvents>();
            });
        var client = host.GetTestServer().CreateClient();
        var state = host.Services.GetRequiredService<RevocationState>();

        var (refreshCookie, sessionId) = await SignInAndRegisterAsync(client);

        var beforeRevoke = await SendRefreshAsync(client, refreshCookie, sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, beforeRevoke.StatusCode);

        state.Revoked = true;
        var afterRevoke = await SendRefreshAsync(client, refreshCookie, sessionId);
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendRefreshAsync(HttpClient client, string refreshCookie, string sessionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
        request.Headers.TryAddWithoutValidation("Cookie", refreshCookie);
        request.Headers.TryAddWithoutValidation(DeviceBoundSessionConstants.Headers.SessionId, sessionId);
        return await client.SendAsync(request);
    }

    private static async Task<(string refreshCookie, string sessionId)> SignInAndRegisterAsync(HttpClient client)
    {
        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();

        var sourceCookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(SourceCookieName + "=", StringComparison.Ordinal));
        var challenge = ParseChallenge(signIn);

        var proof = DbscProofKey.CreateEs256().CreateProof(challenge);
        var register = new HttpRequestMessage(HttpMethod.Post, RegistrationPath);
        register.Headers.TryAddWithoutValidation("Cookie", CookiePair(sourceCookie));
        register.Headers.TryAddWithoutValidation(DeviceBoundSessionConstants.Headers.Proof, proof);
        var response = await client.SendAsync(register);
        response.EnsureSuccessStatusCode();

        var refreshSetCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        var body = await response.Content.ReadAsStringAsync();
        var sessionId = JsonDocument.Parse(body).RootElement.GetProperty("session_identifier").GetString()!;

        return (CookiePair(refreshSetCookie), sessionId);
    }

    private static async Task<IHost> CreateHostAsync(
        Action<CookieAuthenticationOptions> configureSource,
        Action<IServiceCollection>? configureServices = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddDataProtection();
                        configureServices?.Invoke(services);
                        services.AddAuthentication(SourceScheme)
                            .AddCookie(SourceScheme, o =>
                            {
                                o.Cookie.Name = SourceCookieName;
                                configureSource(o);
                            })
                            .AddDeviceBoundSession(SourceScheme);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/signin", async context =>
                            {
                                var identity = new ClaimsIdentity(SourceScheme);
                                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "alice"));
                                await context.SignInAsync(SourceScheme, new ClaimsPrincipal(identity));
                            });
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static string CookiePair(string setCookie)
    {
        var semicolon = setCookie.IndexOf(';');
        return semicolon < 0 ? setCookie : setCookie[..semicolon];
    }

    private static string ParseChallenge(HttpResponseMessage response)
    {
        var header = Assert.Single(response.Headers.GetValues(DeviceBoundSessionConstants.Headers.Registration));
        var match = Regex.Match(header, "challenge=\"([^\"]+)\"");
        Assert.True(match.Success, $"No challenge found in registration header: {header}");
        return match.Groups[1].Value;
    }

    private sealed class RevocationState
    {
        public bool Revoked { get; set; }
    }

    private sealed class RevokingEvents : CookieAuthenticationEvents
    {
        private readonly RevocationState _state;

        public RevokingEvents(RevocationState state)
        {
            _state = state;
        }

        public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            if (_state.Revoked)
            {
                context.RejectPrincipal();
            }
            return Task.CompletedTask;
        }
    }
}
