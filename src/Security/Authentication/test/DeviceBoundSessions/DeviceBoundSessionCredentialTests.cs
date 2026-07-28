// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;
using HttpSameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionCredentialTests
{
    private const string SourceScheme = "Source";
    private const string SessionScheme = SourceScheme + ".Dbsc.Session";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string SessionCookieName = ".AspNetCore.Source.Dbsc.Session";
    private const string RegistrationPath = "/.well-known/dbsc/registration";
    private const string ExpectedAttributes = "domain=example.com; path=/custom; secure; samesite=none; httponly";

    [Fact]
    public async Task Registration_CredentialAttributes_UseEffectiveSessionCookieAttributes()
    {
        using var host = await CreateHostAsync(
            configureSource: ConfigureFiveCookieAttributes,
            configureSession: options => options.Cookie.Path = "/custom");
        var response = await SignInAndRegisterAsync(host);

        var attributes = ParseCredentialAttributes(response);
        var actual = ParseSessionSetCookie(response);
        var advertised = SetCookieHeaderValue.Parse($"ignored=ignored; {attributes}");

        Assert.Equal(ExpectedAttributes, attributes);
        Assert.Equal(actual.Domain, advertised.Domain);
        Assert.Equal(actual.Path, advertised.Path);
        Assert.Equal(actual.Secure, advertised.Secure);
        Assert.Equal(actual.HttpOnly, advertised.HttpOnly);
        Assert.Equal(actual.SameSite, advertised.SameSite);
    }

    [Fact]
    public async Task Registration_CredentialAttributes_OmitLifetimeAndExtensions()
    {
        var maxAge = TimeSpan.FromMinutes(10);
        using var host = await CreateHostAsync(
            configureSource: ConfigureFiveCookieAttributes,
            configureSession: options =>
            {
                options.Cookie.Path = "/custom";
                options.Cookie.MaxAge = maxAge;
                options.Cookie.Extensions.Add("Priority=High");
                options.Cookie.Extensions.Add("Partitioned");
            });

        var resolvedOptions = host.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(SessionScheme);
        Assert.Equal(maxAge, resolvedOptions.Cookie.MaxAge);
        Assert.Equal(["Priority=High", "Partitioned"], resolvedOptions.Cookie.Extensions);

        var response = await SignInAndRegisterAsync(host);
        var attributes = ParseCredentialAttributes(response);
        var sessionSetCookie = GetSessionSetCookie(response);
        var actual = SetCookieHeaderValue.Parse(sessionSetCookie);

        Assert.Equal(ExpectedAttributes, attributes);
        Assert.NotNull(actual.Expires);
        Assert.Equal(maxAge, actual.MaxAge);
        Assert.Contains("expires=", sessionSetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=600", sessionSetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Priority=High", sessionSetCookie, StringComparison.Ordinal);
        Assert.Contains("Partitioned", sessionSetCookie, StringComparison.Ordinal);

        Assert.False(attributes.StartsWith(SessionCookieName + "=", StringComparison.Ordinal));
        Assert.DoesNotContain(actual.Value.Value!, attributes, StringComparison.Ordinal);
        Assert.DoesNotContain("expires=", attributes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("max-age=", attributes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("priority", attributes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("partitioned", attributes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Registration_CredentialPath_MatchesSessionCookie_WithPathBase()
    {
        using var host = await CreateHostAsync(pathBase: "/foo");
        var response = await SignInAndRegisterAsync(host, pathBase: "/foo");

        var attributes = ParseCredentialAttributes(response);
        var actual = ParseSessionSetCookie(response);
        var advertised = SetCookieHeaderValue.Parse($"ignored=ignored; {attributes}");

        Assert.Contains("path=/foo", attributes, StringComparison.Ordinal);
        Assert.DoesNotContain("Path=/foo", attributes, StringComparison.Ordinal);
        Assert.Equal("/foo", actual.Path.Value);
        Assert.Equal(actual.Path, advertised.Path);
    }

    private static void ConfigureFiveCookieAttributes(CookieAuthenticationOptions options)
    {
        options.Cookie.Domain = "example.com";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = HttpSameSiteMode.None;
    }

    private static async Task<IHost> CreateHostAsync(
        Action<CookieAuthenticationOptions>? configureSource = null,
        Action<CookieAuthenticationOptions>? configureSession = null,
        string? pathBase = null)
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
                        var builder = services.AddAuthentication(SourceScheme)
                            .AddCookie(SourceScheme, options =>
                            {
                                options.Cookie.Name = SourceCookieName;
                                configureSource?.Invoke(options);
                            });
                        builder.AddDeviceBoundSession(SourceScheme);
                        services.Configure(SessionScheme, configureSession ?? (_ => { }));
                    })
                    .Configure(app =>
                    {
                        if (pathBase is not null)
                        {
                            app.UsePathBase(pathBase);
                        }

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

    private static async Task<HttpResponseMessage> SignInAndRegisterAsync(IHost host, string pathBase = "")
    {
        var client = host.GetTestServer().CreateClient();
        var signIn = await client.GetAsync(new PathString(pathBase).Add("/signin").ToUriComponent());
        signIn.EnsureSuccessStatusCode();

        var sourceSetCookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(SourceCookieName + "=", StringComparison.Ordinal));
        var challenge = ParseChallenge(signIn);
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            new PathString(pathBase).Add(RegistrationPath).ToUriComponent());
        request.Headers.TryAddWithoutValidation("Cookie", CookiePair(sourceSetCookie));
        request.Headers.TryAddWithoutValidation(
            DeviceBoundSessionConstants.Headers.Proof,
            DbscProofKey.CreateEs256().CreateProof(challenge));

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static string ParseCredentialAttributes(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(response.Content.ReadAsStream());
        return document.RootElement
            .GetProperty("credentials")[0]
            .GetProperty("attributes")
            .GetString()!;
    }

    private static SetCookieHeaderValue ParseSessionSetCookie(HttpResponseMessage response)
        => SetCookieHeaderValue.Parse(GetSessionSetCookie(response));

    private static string GetSessionSetCookie(HttpResponseMessage response)
        => Assert.Single(response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(SessionCookieName + "=", StringComparison.Ordinal));

    private static string CookiePair(string setCookie)
    {
        var cookie = SetCookieHeaderValue.Parse(setCookie);
        return $"{cookie.Name}={cookie.Value}";
    }

    private static string ParseChallenge(HttpResponseMessage response)
    {
        var header = Assert.Single(response.Headers.GetValues(DeviceBoundSessionConstants.Headers.Registration));
        var match = Regex.Match(header, "challenge=\"([^\"]+)\"");
        Assert.True(match.Success, "No challenge found in registration header.");
        return match.Groups[1].Value;
    }
}