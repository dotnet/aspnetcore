// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
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

public class DeviceBoundSessionSignOutTests
{
    private const string SourceScheme = "Source";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string SessionCookieName = ".AspNetCore.Source.Dbsc.Session";
    private const string RefreshCookieName = ".AspNetCore.Source.Dbsc.Refresh";
    private const string RegistrationPath = "/.well-known/dbsc/registration";

    [Fact]
    public async Task Registration_MintsDerivedCookies_WithoutClearingThem()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        var registration = await SignInAndRegisterAsync(client);

        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        var session = GetSetCookie(registration, SessionCookieName);
        var refresh = GetSetCookie(registration, RefreshCookieName);

        // Registration is a cookie exchange: it must SET the derived cookies, not delete them
        // (regression guard — the source sign-out that drops the long-lived cookie must not wipe
        // the session/refresh cookies just minted).
        Assert.NotNull(session);
        Assert.NotNull(refresh);
        Assert.False(IsDeletion(session!));
        Assert.False(IsDeletion(refresh!));
    }

    [Fact]
    public async Task SignOut_OfSourceScheme_ClearsDerivedSessionAndRefreshCookies()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        var registration = await SignInAndRegisterAsync(client);
        registration.EnsureSuccessStatusCode();

        var sessionPair = CookiePair(GetSetCookie(registration, SessionCookieName)!);
        var refreshPair = CookiePair(GetSetCookie(registration, RefreshCookieName)!);

        var signOut = new HttpRequestMessage(HttpMethod.Get, "/signout");
        signOut.Headers.TryAddWithoutValidation("Cookie", $"{sessionPair}; {refreshPair}");
        var signOutResponse = await client.SendAsync(signOut);

        var sessionDeletion = GetSetCookie(signOutResponse, SessionCookieName);
        var refreshDeletion = GetSetCookie(signOutResponse, RefreshCookieName);

        // Signing out only the source scheme must also clear the DBSC-derived cookies, so the
        // application never needs to know the derived scheme names to log the user out.
        Assert.NotNull(sessionDeletion);
        Assert.NotNull(refreshDeletion);
        Assert.True(IsDeletion(sessionDeletion!));
        Assert.True(IsDeletion(refreshDeletion!));
    }

    private static async Task<HttpResponseMessage> SignInAndRegisterAsync(HttpClient client)
    {
        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();

        var sourceCookie = GetSetCookie(signIn, SourceCookieName);
        Assert.NotNull(sourceCookie);
        var challenge = ParseChallenge(signIn);

        var proof = DbscProofKey.CreateEs256().CreateProof(challenge);
        var register = new HttpRequestMessage(HttpMethod.Post, RegistrationPath);
        register.Headers.TryAddWithoutValidation("Cookie", CookiePair(sourceCookie!));
        register.Headers.TryAddWithoutValidation(DeviceBoundSessionConstants.Headers.Proof, proof);
        return await client.SendAsync(register);
    }

    private static async Task<IHost> CreateHostAsync()
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
                        services.AddAuthentication(SourceScheme)
                            .AddCookie(SourceScheme, o => o.Cookie.Name = SourceCookieName)
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

                            endpoints.MapGet("/signout", context => context.SignOutAsync(SourceScheme));
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static string? GetSetCookie(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var value in values)
        {
            if (value.StartsWith(cookieName + "=", StringComparison.Ordinal))
            {
                return value;
            }
        }

        return null;
    }

    private static string CookiePair(string setCookie)
    {
        var semicolon = setCookie.IndexOf(';');
        return semicolon < 0 ? setCookie : setCookie[..semicolon];
    }

    private static bool IsDeletion(string setCookie)
        => setCookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);

    private static string ParseChallenge(HttpResponseMessage response)
    {
        var header = Assert.Single(response.Headers.GetValues(DeviceBoundSessionConstants.Headers.Registration));
        var match = Regex.Match(header, "challenge=\"([^\"]+)\"");
        Assert.True(match.Success, $"No challenge found in registration header: {header}");
        return match.Groups[1].Value;
    }
}
