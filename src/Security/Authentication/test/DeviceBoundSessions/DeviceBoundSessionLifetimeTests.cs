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
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionLifetimeTests
{
    private const string SourceScheme = "Source";
    private const string DbscScheme = DeviceBoundSessionDefaults.AuthenticationScheme;
    private const string RefreshScheme = SourceScheme + ".Dbsc.Refresh";
    private const string SessionScheme = SourceScheme + ".Dbsc.Session";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string RefreshCookieName = ".AspNetCore.Source.Dbsc.Refresh";
    private const string SessionCookieName = ".AspNetCore.Source.Dbsc.Session";
    private const string RegistrationPath = "/.well-known/dbsc/registration";
    private const string RefreshPath = "/.well-known/dbsc/refresh";
    private const string ParameterKey = "TransientParameter";
    private const string ParameterValue = "request-only";
    private static readonly DateTimeOffset InitialTime = new(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ShortSessionLifetime = TimeSpan.FromMinutes(5);

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Registration_PreservesSourceProperties_AndOverwritesDbscProperties(
        bool isPersistent,
        bool allowRefresh)
    {
        var proofKey = DbscProofKey.CreateEs256();
        var sourceProperties = CreateSourceProperties(
            issuedUtc: InitialTime,
            expiresUtc: InitialTime.AddHours(1),
            isPersistent,
            allowRefresh);
        sourceProperties.RedirectUri = "/source-return";
        sourceProperties.Items["CustomItem"] = "custom-value";
        sourceProperties.Items["DbscPublicKeyJwk"] = "source-public-key";
        sourceProperties.Items["DbscSessionId"] = "source-session-id";
        sourceProperties.Items["DbscAlgorithm"] = "source-algorithm";
        sourceProperties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "access-value" },
            new AuthenticationToken { Name = "refresh_token", Value = "refresh-value" },
        ]);

        string? observedParameter = null;
        using var host = await CreateHostAsync(
            sourceProperties,
            configureRefresh: options => options.Events.OnSigningIn = context =>
            {
                observedParameter = context.Properties.GetParameter<string>(ParameterKey);
                return Task.CompletedTask;
            });
        var client = host.GetTestServer().CreateClient();

        var signIn = await client.GetAsync("/signin");
        Assert.Equal(HttpStatusCode.Found, signIn.StatusCode);
        var sourceSetCookie = GetSetCookie(signIn, SourceCookieName);
        var sourceTicket = Unprotect(host, SourceScheme, sourceSetCookie);

        var registration = await RegisterAsync(client, signIn, proofKey);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        var refreshSetCookie = GetSetCookie(registration, RefreshCookieName);
        var refreshTicket = Unprotect(host, RefreshScheme, refreshSetCookie);

        Assert.Equal(sourceTicket.Properties.IssuedUtc, refreshTicket.Properties.IssuedUtc);
        Assert.Equal(sourceTicket.Properties.ExpiresUtc, refreshTicket.Properties.ExpiresUtc);
        Assert.Equal(isPersistent, refreshTicket.Properties.IsPersistent);
        Assert.Equal(allowRefresh, refreshTicket.Properties.AllowRefresh);
        Assert.Equal("custom-value", refreshTicket.Properties.Items["CustomItem"]);
        Assert.Equal("access-value", refreshTicket.Properties.GetTokenValue("access_token"));
        Assert.Equal("refresh-value", refreshTicket.Properties.GetTokenValue("refresh_token"));
        Assert.Equal(2, refreshTicket.Properties.GetTokens().Count());
        Assert.Null(refreshTicket.Properties.RedirectUri);
        Assert.Equal(proofKey.PublicJwkJson, refreshTicket.Properties.Items["DbscPublicKeyJwk"]);
        Assert.Equal(ParseSessionIdentifier(registrationBody), refreshTicket.Properties.Items["DbscSessionId"]);
        Assert.Equal(proofKey.Algorithm, refreshTicket.Properties.Items["DbscAlgorithm"]);
        Assert.Equal(ParameterValue, observedParameter);
        Assert.Empty(sourceTicket.Properties.Parameters);
        Assert.Empty(refreshTicket.Properties.Parameters);

        var parsedSourceCookie = SetCookieHeaderValue.Parse(sourceSetCookie);
        var parsedRefreshCookie = SetCookieHeaderValue.Parse(refreshSetCookie);
        if (isPersistent)
        {
            Assert.Equal(sourceProperties.ExpiresUtc, parsedSourceCookie.Expires);
            Assert.Equal(sourceProperties.ExpiresUtc, parsedRefreshCookie.Expires);
        }
        else
        {
            Assert.Null(parsedSourceCookie.Expires);
            Assert.Null(parsedRefreshCookie.Expires);
        }
    }

    [Fact]
    public async Task NonSlidingRefresh_PreservesDeadline_AndSessionHasBoundedResidualLifetime()
    {
        var expiry = InitialTime.AddMinutes(30);
        var sourceProperties = CreateSourceProperties(InitialTime, expiry, isPersistent: true, allowRefresh: true);
        using var host = await CreateHostAsync(sourceProperties, slidingExpiration: false);
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        host.TimeProvider.Advance(TimeSpan.FromMinutes(29));

        var registration = await RegisterAsync(client, signIn, proofKey);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        var refreshSetCookie = GetSetCookie(registration, RefreshCookieName);
        var sessionSetCookie = GetSetCookie(registration, SessionCookieName);
        var refreshTicket = Unprotect(host, RefreshScheme, refreshSetCookie);
        var sessionTicket = Unprotect(host, SessionScheme, sessionSetCookie);
        var sessionId = ParseSessionIdentifier(registrationBody);

        Assert.Equal(expiry, refreshTicket.Properties.ExpiresUtc);
        Assert.NotEqual(host.TimeProvider.GetUtcNow().AddHours(1), refreshTicket.Properties.ExpiresUtc);
        Assert.Equal(InitialTime.AddMinutes(29).Add(ShortSessionLifetime), sessionTicket.Properties.ExpiresUtc);

        var beforeExpiry = await SendRefreshAsync(client, CookiePair(refreshSetCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, beforeExpiry.StatusCode);
        _ = await beforeExpiry.Content.ReadAsByteArrayAsync();

        host.TimeProvider.Advance(TimeSpan.FromMinutes(1));
        var atExpiry = await SendRefreshAsync(client, CookiePair(refreshSetCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, atExpiry.StatusCode);
        _ = await atExpiry.Content.ReadAsByteArrayAsync();

        host.TimeProvider.Advance(TimeSpan.FromSeconds(1));
        var afterExpiry = await SendRefreshAsync(client, CookiePair(refreshSetCookie), sessionId);
        Assert.Equal(HttpStatusCode.Unauthorized, afterExpiry.StatusCode);
        Assert.Null(TryGetSetCookie(afterExpiry, SessionCookieName));

        host.TimeProvider.Advance(sessionTicket.Properties.ExpiresUtc!.Value - host.TimeProvider.GetUtcNow());
        var sessionAtExpiry = await SendAuthenticatedEndpointAsync(client, CookiePair(sessionSetCookie));
        Assert.Equal(HttpStatusCode.OK, sessionAtExpiry.StatusCode);

        host.TimeProvider.Advance(TimeSpan.FromSeconds(1));
        var sessionAfterExpiry = await SendAuthenticatedEndpointAsync(client, CookiePair(sessionSetCookie));
        Assert.Equal(HttpStatusCode.Unauthorized, sessionAfterExpiry.StatusCode);
    }

    [Fact]
    public async Task SlidingRefresh_RenewsOnProoflessForbiddenResponse()
    {
        var sourceProperties = CreateSourceProperties(
            InitialTime,
            InitialTime.AddMinutes(20),
            isPersistent: true,
            allowRefresh: true);
        using var host = await CreateHostAsync(sourceProperties, slidingExpiration: true);
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        var registration = await RegisterAsync(client, signIn, proofKey);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        var originalRefreshSetCookie = GetSetCookie(registration, RefreshCookieName);
        var originalTicket = Unprotect(host, RefreshScheme, originalRefreshSetCookie);

        host.TimeProvider.Advance(TimeSpan.FromMinutes(11));
        var response = await SendRefreshAsync(
            client,
            CookiePair(originalRefreshSetCookie),
            ParseSessionIdentifier(registrationBody));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _ = await response.Content.ReadAsByteArrayAsync();

        var renewedRefreshSetCookie = GetSetCookie(response, RefreshCookieName);
        var renewedTicket = Unprotect(host, RefreshScheme, renewedRefreshSetCookie);
        Assert.Equal(host.TimeProvider.GetUtcNow(), renewedTicket.Properties.IssuedUtc);
        Assert.Equal(
            originalTicket.Properties.ExpiresUtc - originalTicket.Properties.IssuedUtc,
            renewedTicket.Properties.ExpiresUtc - renewedTicket.Properties.IssuedUtc);
    }

    [Fact]
    public async Task SlidingRefresh_DoesNotRenew_WhenAllowRefreshIsFalse()
    {
        var sourceProperties = CreateSourceProperties(
            InitialTime,
            InitialTime.AddMinutes(20),
            isPersistent: true,
            allowRefresh: false);
        using var host = await CreateHostAsync(sourceProperties, slidingExpiration: true);
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        var registration = await RegisterAsync(client, signIn, proofKey);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        var refreshSetCookie = GetSetCookie(registration, RefreshCookieName);

        host.TimeProvider.Advance(TimeSpan.FromMinutes(11));
        var response = await SendRefreshAsync(
            client,
            CookiePair(refreshSetCookie),
            ParseSessionIdentifier(registrationBody));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _ = await response.Content.ReadAsByteArrayAsync();

        Assert.Null(TryGetSetCookie(response, RefreshCookieName));
    }

    [Fact]
    public async Task SessionCookies_UseExactTimeProviderTimestamps_OnRegistrationAndRefresh()
    {
        var sourceProperties = CreateSourceProperties(
            InitialTime,
            InitialTime.AddHours(1),
            isPersistent: true,
            allowRefresh: true);
        using var host = await CreateHostAsync(sourceProperties);
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        var registration = await RegisterAsync(client, signIn, proofKey);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        var registrationSession = Unprotect(host, SessionScheme, GetSetCookie(registration, SessionCookieName));
        var refreshSetCookie = GetSetCookie(registration, RefreshCookieName);
        var sessionId = ParseSessionIdentifier(registrationBody);

        Assert.Equal(InitialTime, registrationSession.Properties.IssuedUtc);
        Assert.Equal(InitialTime.Add(ShortSessionLifetime), registrationSession.Properties.ExpiresUtc);

        host.TimeProvider.Advance(TimeSpan.FromMinutes(10));
        var challengeResponse = await SendRefreshAsync(client, CookiePair(refreshSetCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponse.StatusCode);
        var challenge = ParseChallenge(challengeResponse, DeviceBoundSessionConstants.Headers.Challenge);
        var proof = proofKey.CreateProof(challenge, includeJwkHeader: false);

        var refreshResponse = await SendRefreshAsync(client, CookiePair(refreshSetCookie), sessionId, proof);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshedSession = Unprotect(host, SessionScheme, GetSetCookie(refreshResponse, SessionCookieName));

        Assert.Equal(host.TimeProvider.GetUtcNow(), refreshedSession.Properties.IssuedUtc);
        Assert.Equal(host.TimeProvider.GetUtcNow().Add(ShortSessionLifetime), refreshedSession.Properties.ExpiresUtc);
        Assert.Equal(
            ShortSessionLifetime,
            refreshedSession.Properties.ExpiresUtc - refreshedSession.Properties.IssuedUtc);
    }

    private static AuthenticationProperties CreateSourceProperties(
        DateTimeOffset issuedUtc,
        DateTimeOffset expiresUtc,
        bool isPersistent,
        bool allowRefresh)
        => new()
        {
            IssuedUtc = issuedUtc,
            ExpiresUtc = expiresUtc,
            IsPersistent = isPersistent,
            AllowRefresh = allowRefresh,
        };

    private static async Task<TestHost> CreateHostAsync(
        AuthenticationProperties sourceProperties,
        bool slidingExpiration = false,
        Action<CookieAuthenticationOptions>? configureRefresh = null)
    {
        var fakeTimeProvider = new FakeTimeProvider(InitialTime);
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddDataProtection();
                        services.AddSingleton<TimeProvider>(fakeTimeProvider);
                        services.AddAuthentication(SourceScheme)
                            .AddCookie(SourceScheme, options =>
                            {
                                options.Cookie.Name = SourceCookieName;
                                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                                options.SlidingExpiration = slidingExpiration;
                            })
                            .AddDeviceBoundSession(SourceScheme, options =>
                            {
                                options.ShortLivedCookieExpiration = ShortSessionLifetime;
                            });

                        services.Configure<CookieAuthenticationOptions>(SourceScheme, options =>
                        {
                            options.TimeProvider = fakeTimeProvider;
                            options.Events.OnValidatePrincipal = context =>
                            {
                                if (context.Request.Path == RegistrationPath)
                                {
                                    context.Properties.SetParameter(ParameterKey, ParameterValue);
                                }

                                return Task.CompletedTask;
                            };
                        });
                        services.Configure<DeviceBoundSessionOptions>(DbscScheme, options =>
                        {
                            options.TimeProvider = fakeTimeProvider;
                        });
                        services.Configure<CookieAuthenticationOptions>(RefreshScheme, options =>
                        {
                            options.TimeProvider = fakeTimeProvider;
                            configureRefresh?.Invoke(options);
                        });
                        services.Configure<CookieAuthenticationOptions>(SessionScheme, options =>
                        {
                            options.TimeProvider = fakeTimeProvider;
                        });
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
                                await context.SignInAsync(
                                    SourceScheme,
                                    new ClaimsPrincipal(identity),
                                    sourceProperties.Clone());
                            });
                            endpoints.MapGet("/authenticated", async context =>
                            {
                                if (context.User.Identity?.IsAuthenticated == true)
                                {
                                    await context.Response.WriteAsync("alice");
                                }
                                else
                                {
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                }
                            });
                        });
                    });
            })
            .Build();

        await host.StartAsync();

        var cookieOptions = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var dbscOptions = host.Services.GetRequiredService<IOptionsMonitor<DeviceBoundSessionOptions>>();
        Assert.Same(fakeTimeProvider, host.Services.GetRequiredService<TimeProvider>());
        Assert.Same(fakeTimeProvider, cookieOptions.Get(SourceScheme).TimeProvider);
        Assert.Same(fakeTimeProvider, dbscOptions.Get(DbscScheme).TimeProvider);
        Assert.Same(fakeTimeProvider, cookieOptions.Get(RefreshScheme).TimeProvider);
        Assert.Same(fakeTimeProvider, cookieOptions.Get(SessionScheme).TimeProvider);

        return new TestHost(host, fakeTimeProvider);
    }

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        HttpResponseMessage signIn,
        DbscProofKey proofKey)
    {
        var sourceSetCookie = GetSetCookie(signIn, SourceCookieName);
        var challenge = ParseChallenge(signIn, DeviceBoundSessionConstants.Headers.Registration);
        var request = new HttpRequestMessage(HttpMethod.Post, RegistrationPath);
        request.Headers.TryAddWithoutValidation("Cookie", CookiePair(sourceSetCookie));
        request.Headers.TryAddWithoutValidation(
            DeviceBoundSessionConstants.Headers.Proof,
            proofKey.CreateProof(challenge));
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static async Task<HttpResponseMessage> SendRefreshAsync(
        HttpClient client,
        string refreshCookie,
        string sessionId,
        string? proof = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
        request.Headers.TryAddWithoutValidation("Cookie", refreshCookie);
        request.Headers.TryAddWithoutValidation(DeviceBoundSessionConstants.Headers.SessionId, sessionId);
        if (proof is not null)
        {
            request.Headers.TryAddWithoutValidation(DeviceBoundSessionConstants.Headers.Proof, proof);
        }

        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendAuthenticatedEndpointAsync(HttpClient client, string sessionCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/authenticated");
        request.Headers.TryAddWithoutValidation("Cookie", sessionCookie);
        return client.SendAsync(request);
    }

    private static AuthenticationTicket Unprotect(TestHost host, string scheme, string setCookie)
    {
        var options = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(scheme);
        var cookie = SetCookieHeaderValue.Parse(setCookie);
        return Assert.IsType<AuthenticationTicket>(options.TicketDataFormat.Unprotect(cookie.Value.ToString()));
    }

    private static string GetSetCookie(HttpResponseMessage response, string cookieName)
        => Assert.Single(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(cookieName + "=", StringComparison.Ordinal));

    private static string? TryGetSetCookie(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        return values.SingleOrDefault(value => value.StartsWith(cookieName + "=", StringComparison.Ordinal));
    }

    private static string CookiePair(string setCookie)
    {
        var cookie = SetCookieHeaderValue.Parse(setCookie);
        return $"{cookie.Name}={cookie.Value}";
    }

    private static string ParseChallenge(HttpResponseMessage response, string headerName)
    {
        var header = Assert.Single(response.Headers.GetValues(headerName));
        var pattern = headerName == DeviceBoundSessionConstants.Headers.Challenge
            ? "^\"([^\"]+)\";id="
            : "challenge=\"([^\"]+)\"";
        var match = Regex.Match(header, pattern);
        Assert.True(match.Success, $"No challenge found in {headerName} header.");
        return match.Groups[1].Value;
    }

    private static string ParseSessionIdentifier(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("session_identifier").GetString()!;
    }

    private sealed class TestHost : IDisposable
    {
        private readonly IHost _host;

        public TestHost(IHost host, FakeTimeProvider timeProvider)
        {
            _host = host;
            TimeProvider = timeProvider;
        }

        public FakeTimeProvider TimeProvider { get; }

        public IServiceProvider Services => _host.Services;

        public TestServer GetTestServer() => _host.GetTestServer();

        public void Dispose() => _host.Dispose();
    }
}
