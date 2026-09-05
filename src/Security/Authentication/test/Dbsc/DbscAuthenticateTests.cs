// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

public class DbscAuthenticateTests
{
    private const string SourceScheme = "Source";
    private const string DbscScheme = DbscDefaults.AuthenticationScheme;
    private const string SessionScheme = DbscScheme + ".Session";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string SessionCookieName = ".AspNetCore.DBSC.Session";
    private const string OriginItemKey = "origin";

    [Fact]
    public async Task AuthenticateAsync_PrefersSessionTicket_AndRestampsScheme()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();
        var sourceCookie = await SignInAsync(client, "/signin/source", SourceCookieName);
        var sessionCookie = await SignInAsync(client, "/signin/session", SessionCookieName);

        await CaptureAuthenticateResultAsync(client, sourceCookie, sessionCookie);

        AssertAuthenticationResult(host, "session-user", "session");
    }

    [Fact]
    public async Task AuthenticateAsync_FallsBackToSourceTicket_WhenSessionCookieIsMissing()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();
        var sourceCookie = await SignInAsync(client, "/signin/source", SourceCookieName);

        await CaptureAuthenticateResultAsync(client, sourceCookie);

        AssertAuthenticationResult(host, "source-user", "source");
    }

    [Fact]
    public async Task AuthenticateAsync_FallsBackToSourceTicket_WhenSessionCookieIsInvalid()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();
        var sourceCookie = await SignInAsync(client, "/signin/source", SourceCookieName);

        await CaptureAuthenticateResultAsync(client, $"{SessionCookieName}=invalid", sourceCookie);

        AssertAuthenticationResult(host, "source-user", "source");
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNoResult_WhenCookiesAreMissing()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        await CaptureAuthenticateResultAsync(client);

        var result = GetCapturedResult(host);
        Assert.True(result.None);
        Assert.False(result.Succeeded);
        Assert.Null(result.Ticket);
    }

    [Fact]
    public async Task ExplicitDbscScheme_AuthorizesEndpoint()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();
        var sessionCookie = await SignInAsync(client, "/signin/session", SessionCookieName);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/explicit");
        request.Headers.TryAddWithoutValidation("Cookie", sessionCookie);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("session-user", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SourceSchemeConfiguredInOptions_IsResolvedLazily()
    {
        const string configuredSourceScheme = "ConfiguredSource";
        const string configuredSourceCookieName = ".AspNetCore.ConfiguredSource";
        using var host = await CreateHostAsync(configuredSourceScheme, configuredSourceCookieName);
        var client = host.GetTestServer().CreateClient();

        using var signIn = await client.GetAsync("/signin/source");
        signIn.EnsureSuccessStatusCode();
        Assert.True(signIn.Headers.Contains(DbscConstants.Headers.Registration));
        var setCookie = Assert.Single(
            signIn.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(configuredSourceCookieName + "=", StringComparison.Ordinal));
        var cookie = SetCookieHeaderValue.Parse(setCookie);

        await CaptureAuthenticateResultAsync(client, $"{cookie.Name}={cookie.Value}");

        AssertAuthenticationResult(host, "source-user", "source");
    }

    [Fact]
    public async Task ForwardAuthenticate_UsesForwardedScheme()
    {
        using var host = await CreateHostAsync(configureDbsc: options => options.ForwardAuthenticate = SourceScheme);
        var client = host.GetTestServer().CreateClient();
        var sourceCookie = await SignInAsync(client, "/signin/source", SourceCookieName);
        var sessionCookie = await SignInAsync(client, "/signin/session", SessionCookieName);

        await CaptureAuthenticateResultAsync(client, sourceCookie, sessionCookie);

        AssertAuthenticationResult(host, "source-user", "source", SourceScheme);
    }

    private static async Task<IHost> CreateHostAsync(
        string sourceScheme = SourceScheme,
        string sourceCookieName = SourceCookieName,
        Action<DbscOptions>? configureDbsc = null)
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
                        services.AddAuthorization();
                        services.AddSingleton<AuthenticateResultCapture>();
                        services.AddAuthentication(sourceScheme)
                            .AddCookie(sourceScheme, options => options.Cookie.Name = sourceCookieName)
                            .AddDbsc(DbscScheme, options =>
                            {
                                options.SourceScheme = sourceScheme;
                                configureDbsc?.Invoke(options);
                            });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/signin/source", context =>
                                SignInAsync(context, sourceScheme, "source-user", "source"));
                            endpoints.MapGet("/signin/session", context =>
                                SignInAsync(context, SessionScheme, "session-user", "session"));
                            endpoints.MapGet("/authenticate", async context =>
                            {
                                var capture = context.RequestServices.GetRequiredService<AuthenticateResultCapture>();
                                capture.Result = await context.AuthenticateAsync(DbscScheme);
                                context.Response.StatusCode = StatusCodes.Status204NoContent;
                            });
                            endpoints.MapGet("/explicit", async context =>
                            {
                                var nameIdentifier = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                                await context.Response.WriteAsync(nameIdentifier ?? string.Empty);
                            }).RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = DbscScheme });
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static Task SignInAsync(
        HttpContext context,
        string scheme,
        string nameIdentifier,
        string origin)
    {
        var identity = new ClaimsIdentity(scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        var properties = new AuthenticationProperties();
        properties.Items[OriginItemKey] = origin;
        return context.SignInAsync(scheme, new ClaimsPrincipal(identity), properties);
    }

    private static async Task<string> SignInAsync(HttpClient client, string path, string cookieName)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var setCookie = Assert.Single(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(cookieName + "=", StringComparison.Ordinal));
        var cookie = SetCookieHeaderValue.Parse(setCookie);
        return $"{cookie.Name}={cookie.Value}";
    }

    private static async Task CaptureAuthenticateResultAsync(HttpClient client, params string[] cookies)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/authenticate");
        if (cookies.Length > 0)
        {
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        }

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static void AssertAuthenticationResult(
        IHost host,
        string expectedNameIdentifier,
        string expectedOrigin,
        string expectedScheme = DbscScheme)
    {
        var result = GetCapturedResult(host);
        Assert.True(result.Succeeded);
        var ticket = Assert.IsType<AuthenticationTicket>(result.Ticket);
        Assert.Same(ticket.Principal, result.Principal);
        Assert.Equal(expectedScheme, ticket.AuthenticationScheme);
        Assert.Equal(expectedNameIdentifier, ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(expectedOrigin, ticket.Properties.Items[OriginItemKey]);
    }

    private static AuthenticateResult GetCapturedResult(IHost host)
        => Assert.IsType<AuthenticateResult>(host.Services.GetRequiredService<AuthenticateResultCapture>().Result);

    private sealed class AuthenticateResultCapture
    {
        public AuthenticateResult? Result { get; set; }
    }
}
