// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0030 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionInstructionTests
{
    private const string SourceScheme = "Source";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string RefreshCookieName = ".AspNetCore.Source.Dbsc.Refresh";
    private const string SessionCookieName = ".AspNetCore.Source.Dbsc.Session";
    private const string RegistrationPath = "/.well-known/dbsc/registration";
    private const string RefreshPath = "/.well-known/dbsc/refresh";
    private const string RootHostOrigin = "https://example.com";

    [Fact]
    public void Continue_DefaultsToTrue_AndIsAlwaysSerialized()
    {
        var json = Serialize(new SessionInstruction { SessionIdentifier = "id" });

        Assert.Contains("\"continue\":true", json);
    }

    [Fact]
    public void AllowedRefreshInitiators_AreSerialized_WhenSet()
    {
        var json = Serialize(new SessionInstruction
        {
            SessionIdentifier = "id",
            AllowedRefreshInitiators = ["example.com", "*.example.com"],
        });

        Assert.Contains("\"allowed_refresh_initiators\":[\"example.com\",\"*.example.com\"]", json);
    }

    [Fact]
    public void AllowedRefreshInitiators_AreSerialized_AsEmptyArray_WhenEmpty()
    {
        var json = Serialize(new SessionInstruction { SessionIdentifier = "id" });

        Assert.Contains("\"allowed_refresh_initiators\":[]", json);
    }

    [Fact]
    public async Task Registration_FlowsAllowedRefreshInitiators_FromOptions()
    {
        using var host = await CreateHostAsync(o =>
        {
            o.AllowedRefreshInitiators.Add("example.com");
            o.AllowedRefreshInitiators.Add("*.example.com");
        });
        var client = host.GetTestServer().CreateClient();

        var body = await SignInRegisterAndReadBodyAsync(client);

        // Setting the option must actually flow into the session instruction JSON.
        Assert.Contains("\"allowed_refresh_initiators\":[\"example.com\",\"*.example.com\"]", body);
        Assert.Contains("\"continue\":true", body);
    }

    [Fact]
    public async Task Registration_SerializesEmptyAllowedRefreshInitiators_WhenNotConfigured()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        var body = await SignInRegisterAndReadBodyAsync(client);

        Assert.Contains("\"allowed_refresh_initiators\":[]", body);
    }

    [Fact]
    public async Task Registration_DefaultsIncludeSiteToFalse()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();

        var body = await SignInRegisterAndReadBodyAsync(client);

        AssertScope(body, expectedOrigin: "http://localhost", expectedIncludeSite: false);
    }

    [Fact]
    public async Task Registration_FlowsIncludeSiteForHttpsRootHost()
    {
        using var host = await CreateHostAsync(o => o.IncludeSite = true);
        var client = host.GetTestServer().CreateClient();
        client.BaseAddress = new Uri(RootHostOrigin);

        var body = await SignInRegisterAndReadBodyAsync(client);

        AssertScope(body, expectedOrigin: RootHostOrigin, expectedIncludeSite: true);
    }

    [Fact]
    public async Task SuccessfulRefresh_RepeatsIncludeSiteForHttpsRootHost()
    {
        using var host = await CreateHostAsync(o => o.IncludeSite = true);
        var client = host.GetTestServer().CreateClient();
        client.BaseAddress = new Uri(RootHostOrigin);
        var proofKey = DbscProofKey.CreateEs256();

        var registration = await SignInAndRegisterAsync(client, proofKey);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        var refreshCookie = Assert.Single(registration.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        var sessionId = ParseSessionIdentifier(registrationBody);

        var challengeResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponse.StatusCode);
        var challenge = ParseChallenge(challengeResponse, DeviceBoundSessionConstants.Headers.Challenge);
        var proof = proofKey.CreateProof(challenge, includeJwkHeader: false);

        var refreshResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId, proof);
        refreshResponse.EnsureSuccessStatusCode();
        var refreshBody = await refreshResponse.Content.ReadAsStringAsync();

        AssertScope(refreshBody, expectedOrigin: RootHostOrigin, expectedIncludeSite: true);
    }

    [Fact]
    public async Task Refresh_AcceptsOlderRecentChallenge_AndAllowsProofReuseWhileUnexpired()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var registration = await SignInAndRegisterAsync(client, proofKey);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        var refreshCookie = Assert.Single(registration.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        var sessionId = ParseSessionIdentifier(registrationBody);

        var challengeResponseA = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponseA.StatusCode);
        var challengeA = ParseChallenge(challengeResponseA, DeviceBoundSessionConstants.Headers.Challenge);

        var challengeResponseB = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponseB.StatusCode);
        var challengeB = ParseChallenge(challengeResponseB, DeviceBoundSessionConstants.Headers.Challenge);
        Assert.False(string.Equals(challengeA, challengeB, StringComparison.Ordinal), "Refresh challenges should be distinct.");

        var proof = proofKey.CreateProof(challengeA, includeJwkHeader: false);
        var refreshResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId, proof);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        AssertSessionCookieIssued(refreshResponse);

        var replayResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId, proof);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        AssertSessionCookieIssued(replayResponse);
    }

    [Fact]
    public async Task Refresh_RejectsAlreadyExpiredChallenge_AndIssuesReplacementChallenge()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var registration = await SignInAndRegisterAsync(client, proofKey);
        var registrationBody = await registration.Content.ReadAsStringAsync();
        var refreshCookie = Assert.Single(registration.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        var sessionId = ParseSessionIdentifier(registrationBody);

        var options = host.Services.GetRequiredService<IOptionsMonitor<DeviceBoundSessionOptions>>()
            .Get(DeviceBoundSessionDefaults.AuthenticationScheme);
        options.ChallengeMaxAge = TimeSpan.FromSeconds(-1);

        var challengeResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponse.StatusCode);
        var challenge = ParseChallenge(challengeResponse, DeviceBoundSessionConstants.Headers.Challenge);
        var proof = proofKey.CreateProof(challenge, includeJwkHeader: false);

        var refreshResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId, proof);
        Assert.Equal(HttpStatusCode.Forbidden, refreshResponse.StatusCode);
        _ = ParseChallenge(refreshResponse, DeviceBoundSessionConstants.Headers.Challenge);
    }

    [Fact]
    public async Task Registration_AdvertisesRegistrationPathAndRefreshUrl_WithPathBase()
    {
        using var host = await CreateHostAsync(pathBase: "/foo");
        var client = host.GetTestServer().CreateClient();

        // The registration header advertised on sign-in must include the path base.
        var signIn = await client.GetAsync("/foo/signin");
        signIn.EnsureSuccessStatusCode();
        var registrationHeader = Assert.Single(
            signIn.Headers.GetValues(DeviceBoundSessionConstants.Headers.Registration));
        Assert.Contains("path=\"/foo/.well-known/dbsc/registration\"", registrationHeader);

        // Completing registration returns a refresh_url that also includes the path base.
        var response = await RegisterAsync(client, signIn, DbscProofKey.CreateEs256(), pathBase: "/foo");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"refresh_url\":\"/foo/.well-known/dbsc/refresh\"", body);

        // The refresh cookie is path-scoped under the path base so the browser sends it to the refresh endpoint.
        var refreshSetCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(".AspNetCore.Source.Dbsc.Refresh=", StringComparison.Ordinal));
        Assert.Contains("path=/foo/.well-known/dbsc", refreshSetCookie);

        // The session cookie and the advertised credential attributes both carry the path base.
        var sessionSetCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(".AspNetCore.Source.Dbsc.Session=", StringComparison.Ordinal));
        Assert.Contains("path=/foo", sessionSetCookie);
        Assert.Contains("Path=/foo\"", body);
    }

    private static string Serialize(SessionInstruction instruction)
        => JsonSerializer.Serialize(instruction, DeviceBoundSessionJsonContext.Default.SessionInstruction);

    private static async Task<string> SignInRegisterAndReadBodyAsync(HttpClient client)
    {
        var response = await SignInAndRegisterAsync(client, DbscProofKey.CreateEs256());
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<HttpResponseMessage> SignInAndRegisterAsync(HttpClient client, DbscProofKey proofKey)
    {
        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();

        return await RegisterAsync(client, signIn, proofKey);
    }

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        HttpResponseMessage signIn,
        DbscProofKey proofKey,
        string pathBase = "")
    {
        var sourceCookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(SourceCookieName + "=", StringComparison.Ordinal));
        var challenge = ParseChallenge(signIn, DeviceBoundSessionConstants.Headers.Registration);

        var proof = proofKey.CreateProof(challenge);
        var register = new HttpRequestMessage(HttpMethod.Post, pathBase + RegistrationPath);
        register.Headers.TryAddWithoutValidation("Cookie", CookiePair(sourceCookie));
        register.Headers.TryAddWithoutValidation(DeviceBoundSessionConstants.Headers.Proof, proof);
        var response = await client.SendAsync(register);
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

    private static async Task<IHost> CreateHostAsync(Action<DeviceBoundSessionOptions>? configureDbsc = null, string? pathBase = null)
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
                            .AddCookie(SourceScheme, o => o.Cookie.Name = SourceCookieName);
                        if (configureDbsc is null)
                        {
                            builder.AddDeviceBoundSession(SourceScheme);
                        }
                        else
                        {
                            builder.AddDeviceBoundSession(SourceScheme, configureDbsc);
                        }
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

    private static string CookiePair(string setCookie)
    {
        var semicolon = setCookie.IndexOf(';');
        return semicolon < 0 ? setCookie : setCookie[..semicolon];
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

    private static void AssertSessionCookieIssued(HttpResponseMessage response)
    {
        Assert.Single(response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(SessionCookieName + "=", StringComparison.Ordinal));
    }

    private static string ParseSessionIdentifier(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("session_identifier").GetString()!;
    }

    private static void AssertScope(string body, string expectedOrigin, bool expectedIncludeSite)
    {
        using var document = JsonDocument.Parse(body);
        var scope = document.RootElement.GetProperty("scope");
        Assert.Equal(expectedOrigin, scope.GetProperty("origin").GetString());
        Assert.Equal(expectedIncludeSite, scope.GetProperty("include_site").GetBoolean());
    }
}
