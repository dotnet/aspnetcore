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

namespace Microsoft.AspNetCore.Authentication.Dbsc;

public class DbscSignOutTests
{
    private const string SourceScheme = "Source";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string SessionCookieName = ".AspNetCore.DBSC.Session";
    private const string RefreshCookieName = ".AspNetCore.DBSC.Refresh";
    private const string RegistrationPath = "/.well-known/dbsc/registration";
    private const string RefreshPath = "/.well-known/dbsc/refresh";

    [Fact]
    public async Task SignOut_OfDbscScheme_ThrowsInvalidOperationException()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/signout-dbsc"));

        Assert.Equal(
            $"The authentication handler registered for scheme '{DbscDefaults.AuthenticationScheme}' is '{nameof(DbscHandler)}' which cannot be used for SignOutAsync. The registered sign-out schemes are: {SourceScheme}, {DbscDefaults.AuthenticationScheme}.Refresh, {DbscDefaults.AuthenticationScheme}.Session.",
            exception.Message);
    }

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
    public async Task RegisteredDbscAgent_SignOutOfSourceScheme_ClearsAllCookies_AndUnauthenticatesNextRequest()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        var registration = await SignInAndRegisterAsync(client);
        registration.EnsureSuccessStatusCode();

        var sessionPair = CookiePair(GetSetCookie(registration, SessionCookieName)!);
        var refreshPair = CookiePair(GetSetCookie(registration, RefreshCookieName)!);

        var authenticated = await SendAuthenticateAsync(client, sessionPair);
        Assert.Equal(HttpStatusCode.NoContent, authenticated.StatusCode);

        var signOut = new HttpRequestMessage(HttpMethod.Get, "/signout");
        signOut.Headers.TryAddWithoutValidation("Cookie", $"{sessionPair}; {refreshPair}");
        var signOutResponse = await client.SendAsync(signOut);

        signOutResponse.EnsureSuccessStatusCode();
        AssertCookieDeleted(signOutResponse, SourceCookieName);
        AssertCookieDeleted(signOutResponse, SessionCookieName);
        AssertCookieDeleted(signOutResponse, RefreshCookieName);

        var afterSignOut = await SendAuthenticateAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, afterSignOut.StatusCode);
    }

    [Fact]
    public async Task NonDbscAgent_SignOutOfSourceScheme_ClearsSourceCookie_AndDerivedSignOutsAreNoOps()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        var sourcePair = CookiePair(GetSetCookie(signIn, SourceCookieName)!);

        var authenticated = await SendAuthenticateAsync(client, sourcePair);
        Assert.Equal(HttpStatusCode.NoContent, authenticated.StatusCode);

        var signOut = new HttpRequestMessage(HttpMethod.Get, "/signout");
        signOut.Headers.TryAddWithoutValidation("Cookie", sourcePair);
        var signOutResponse = await client.SendAsync(signOut);

        signOutResponse.EnsureSuccessStatusCode();
        AssertCookieDeleted(signOutResponse, SourceCookieName);
        AssertCookieDeleted(signOutResponse, SessionCookieName);
        AssertCookieDeleted(signOutResponse, RefreshCookieName);

        var afterSignOut = await SendAuthenticateAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, afterSignOut.StatusCode);
    }

    [Fact]
    public async Task MidRegistration_SignOutOfSourceScheme_ClearsAllCookies_AndRefreshCannotMintSession()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        var sourcePair = CookiePair(GetSetCookie(signIn, SourceCookieName)!);
        var challenge = ParseChallenge(signIn);

        var proof = DbscProofKey.CreateEs256().CreateProof(challenge);
        var register = new HttpRequestMessage(HttpMethod.Post, RegistrationPath);
        register.Headers.TryAddWithoutValidation("Cookie", sourcePair);
        register.Headers.TryAddWithoutValidation(DbscConstants.Headers.Proof, proof);
        var registration = await client.SendAsync(register);
        registration.EnsureSuccessStatusCode();

        var sessionPair = CookiePair(GetSetCookie(registration, SessionCookieName)!);
        var refreshPair = CookiePair(GetSetCookie(registration, RefreshCookieName)!);

        var signOut = new HttpRequestMessage(HttpMethod.Get, "/signout");
        signOut.Headers.TryAddWithoutValidation("Cookie", $"{sourcePair}; {sessionPair}; {refreshPair}");
        var signOutResponse = await client.SendAsync(signOut);

        signOutResponse.EnsureSuccessStatusCode();
        AssertCookieDeleted(signOutResponse, SourceCookieName);
        AssertCookieDeleted(signOutResponse, SessionCookieName);
        AssertCookieDeleted(signOutResponse, RefreshCookieName);

        var afterSignOut = await SendAuthenticateAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, afterSignOut.StatusCode);

        var refresh = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
        refresh.Headers.TryAddWithoutValidation(DbscConstants.Headers.SessionId, "signed-out-session");
        var refreshResponse = await client.SendAsync(refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
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
        register.Headers.TryAddWithoutValidation(DbscConstants.Headers.Proof, proof);
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
                            .AddDbsc(
                                DbscDefaults.AuthenticationScheme,
                                options => options.SourceScheme = SourceScheme);
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
                            endpoints.MapGet("/signout-dbsc", context => context.SignOutAsync(DbscDefaults.AuthenticationScheme));
                            endpoints.MapGet("/authenticate", async context =>
                            {
                                var result = await context.AuthenticateAsync(DbscDefaults.AuthenticationScheme);
                                context.Response.StatusCode = result.Succeeded
                                    ? StatusCodes.Status204NoContent
                                    : StatusCodes.Status401Unauthorized;
                            });
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static Task<HttpResponseMessage> SendAuthenticateAsync(HttpClient client, params string[] cookies)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/authenticate");
        if (cookies.Length > 0)
        {
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        }

        return client.SendAsync(request);
    }

    private static void AssertCookieDeleted(HttpResponseMessage response, string cookieName)
    {
        var setCookie = GetSetCookie(response, cookieName);
        Assert.NotNull(setCookie);
        Assert.True(IsDeletion(setCookie));
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
        var header = Assert.Single(response.Headers.GetValues(DbscConstants.Headers.Registration));
        var match = Regex.Match(header, "challenge=\"([^\"]+)\"");
        Assert.True(match.Success, $"No challenge found in registration header: {header}");
        return match.Groups[1].Value;
    }
}
