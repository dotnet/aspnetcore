// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

public class DbscInstructionTests
{
    private const string SourceScheme = "Source";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string RefreshCookieName = ".AspNetCore.DBSC.Refresh";
    private const string SessionCookieName = ".AspNetCore.DBSC.Session";
    private const string RegistrationPath = "/.well-known/dbsc/registration";
    private const string RefreshPath = "/.well-known/dbsc/refresh";
    private const string CustomRegistrationPath = "/custom/dbsc/register";
    private const string CustomRefreshPath = "/custom/dbsc/refresh";
    private const string RootHostOrigin = "https://example.com";

    [Fact]
    public void Continue_DefaultsToTrue_AndIsAlwaysSerialized()
    {
        var json = Serialize(new SessionInstruction { SessionIdentifier = "id" });

        Assert.Equal("{\"session_identifier\":\"id\",\"continue\":true,\"allowed_refresh_initiators\":[]}", json);
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
        var challenge = ParseChallenge(challengeResponse, DbscConstants.Headers.Challenge);
        var proof = proofKey.CreateProof(challenge, includeJwkHeader: false);

        var refreshResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId, proof);
        refreshResponse.EnsureSuccessStatusCode();
        var refreshBody = await refreshResponse.Content.ReadAsStringAsync();

        Assert.Equal(registrationBody, refreshBody);
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
        var challengeA = ParseChallenge(challengeResponseA, DbscConstants.Headers.Challenge);

        var challengeResponseB = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponseB.StatusCode);
        var challengeB = ParseChallenge(challengeResponseB, DbscConstants.Headers.Challenge);
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

        var options = host.Services.GetRequiredService<IOptionsMonitor<DbscOptions>>()
            .Get(DbscDefaults.AuthenticationScheme);
        options.ChallengeMaxAge = TimeSpan.FromSeconds(-1);

        var challengeResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponse.StatusCode);
        var challenge = ParseChallenge(challengeResponse, DbscConstants.Headers.Challenge);
        var proof = proofKey.CreateProof(challenge, includeJwkHeader: false);

        var refreshResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId, proof);
        Assert.Equal(HttpStatusCode.Forbidden, refreshResponse.StatusCode);
        _ = ParseChallenge(refreshResponse, DbscConstants.Headers.Challenge);
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
            signIn.Headers.GetValues(DbscConstants.Headers.Registration));
        Assert.Contains("path=\"/foo/.well-known/dbsc/registration\"", registrationHeader);

        // Completing registration returns a refresh_url that also includes the path base.
        var response = await RegisterAsync(client, signIn, DbscProofKey.CreateEs256(), pathBase: "/foo");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"refresh_url\":\"/foo/.well-known/dbsc/refresh\"", body);

        // The refresh cookie is path-scoped under the path base so the browser sends it to the refresh endpoint.
        var refreshSetCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        Assert.Contains("path=/foo/.well-known/dbsc", refreshSetCookie);

        // The session cookie and the advertised credential attributes both carry the path base.
        var sessionSetCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(SessionCookieName + "=", StringComparison.Ordinal));
        Assert.Contains("path=/foo", sessionSetCookie);
        using var document = JsonDocument.Parse(body);
        var attributes = document.RootElement.GetProperty("credentials")[0].GetProperty("attributes").GetString();
        var advertisedCookie = SetCookieHeaderValue.Parse($"credential=value; {attributes}");
        Assert.Equal("/foo", advertisedCookie.Path.Value);
    }

    [Fact]
    public async Task CustomPaths_AdvertiseDispatchAndScopeRefreshCookie_WithoutHandlingDefaults()
    {
        using var host = await CreateHostAsync(
            registrationPath: CustomRegistrationPath,
            refreshPath: CustomRefreshPath);
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        var registrationHeader = Assert.Single(
            signIn.Headers.GetValues(DbscConstants.Headers.Registration));
        Assert.Contains($"path=\"{CustomRegistrationPath}\"", registrationHeader);

        var registration = await RegisterAsync(
            client,
            signIn,
            proofKey,
            registrationPath: CustomRegistrationPath);
        var body = await registration.Content.ReadAsStringAsync();
        Assert.Contains($"\"refresh_url\":\"{CustomRefreshPath}\"", body);

        var refreshCookie = Assert.Single(registration.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        Assert.Equal("/custom/dbsc", ParseCookiePath(refreshCookie));
        var sessionId = ParseSessionIdentifier(body);

        var refresh = await SendRefreshAsync(
            client,
            CookiePair(refreshCookie),
            sessionId,
            refreshPath: CustomRefreshPath);
        Assert.Equal(HttpStatusCode.Forbidden, refresh.StatusCode);
        _ = ParseChallenge(refresh, DbscConstants.Headers.Challenge);

        var defaultRegistration = await client.PostAsync(RegistrationPath, content: null);
        var defaultRefresh = await client.PostAsync(RefreshPath, content: null);
        Assert.Equal(HttpStatusCode.NotFound, defaultRegistration.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, defaultRefresh.StatusCode);
    }

    [Fact]
    public async Task CustomPaths_AdvertiseDispatchAndScopeRefreshCookie_WithPathBase()
    {
        const string pathBase = "/foo";
        using var host = await CreateHostAsync(
            pathBase: pathBase,
            registrationPath: CustomRegistrationPath,
            refreshPath: CustomRefreshPath);
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var signIn = await client.GetAsync(GetRequestTarget(pathBase, "/signin"));
        signIn.EnsureSuccessStatusCode();
        var registrationHeader = Assert.Single(
            signIn.Headers.GetValues(DbscConstants.Headers.Registration));
        Assert.Contains("path=\"/foo/custom/dbsc/register\"", registrationHeader);
        Assert.DoesNotContain("/foo/foo/", registrationHeader);

        var registration = await RegisterAsync(
            client,
            signIn,
            proofKey,
            pathBase,
            CustomRegistrationPath);
        var body = await registration.Content.ReadAsStringAsync();
        Assert.Contains("\"refresh_url\":\"/foo/custom/dbsc/refresh\"", body);
        Assert.DoesNotContain("/foo/foo/", body);

        var refreshCookie = Assert.Single(registration.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        Assert.Equal("/foo/custom/dbsc", ParseCookiePath(refreshCookie));
        var sessionId = ParseSessionIdentifier(body);

        var refresh = await SendRefreshAsync(
            client,
            CookiePair(refreshCookie),
            sessionId,
            pathBase: pathBase,
            refreshPath: CustomRefreshPath);
        Assert.Equal(HttpStatusCode.Forbidden, refresh.StatusCode);
        _ = ParseChallenge(refresh, DbscConstants.Headers.Challenge);
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("/foo", "/foo/")]
    public async Task RootRefreshPath_DispatchesAndScopesCookieToApplicationRoot(
        string? pathBase,
        string expectedCookiePath)
    {
        using var host = await CreateHostAsync(
            pathBase: pathBase,
            registrationPath: CustomRegistrationPath,
            refreshPath: "/");
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var registration = await SignInAndRegisterAsync(
            client,
            proofKey,
            pathBase ?? string.Empty,
            CustomRegistrationPath);
        var body = await registration.Content.ReadAsStringAsync();
        Assert.Contains($"\"refresh_url\":\"{expectedCookiePath}\"", body);

        var refreshCookie = Assert.Single(registration.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        Assert.Equal(expectedCookiePath, ParseCookiePath(refreshCookie));

        var refresh = await SendRefreshAsync(
            client,
            CookiePair(refreshCookie),
            ParseSessionIdentifier(body),
            pathBase: pathBase ?? string.Empty,
            refreshPath: "/");
        Assert.Equal(HttpStatusCode.Forbidden, refresh.StatusCode);
        _ = ParseChallenge(refresh, DbscConstants.Headers.Challenge);
    }

    [Fact]
    public async Task SpaceAndUnicodePaths_AreEncodedWhenAdvertisedAndDispatchFromEncodedTargets()
    {
        const string registrationPath = "/custom path/café/注册";
        const string refreshPath = "/custom path/café/刷新";
        const string encodedRegistrationPath = "/custom%20path/caf%C3%A9/%E6%B3%A8%E5%86%8C";
        const string encodedRefreshPath = "/custom%20path/caf%C3%A9/%E5%88%B7%E6%96%B0";
        using var host = await CreateHostAsync(
            registrationPath: registrationPath,
            refreshPath: refreshPath);
        var client = host.GetTestServer().CreateClient();
        var proofKey = DbscProofKey.CreateEs256();

        var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        var registrationHeader = Assert.Single(
            signIn.Headers.GetValues(DbscConstants.Headers.Registration));
        Assert.Contains($"path=\"{encodedRegistrationPath}\"", registrationHeader);

        var registration = await RegisterAsync(
            client,
            signIn,
            proofKey,
            registrationPath: registrationPath);
        var body = await registration.Content.ReadAsStringAsync();
        Assert.Contains($"\"refresh_url\":\"{encodedRefreshPath}\"", body);

        var refreshCookie = Assert.Single(registration.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        var refresh = await SendRefreshAsync(
            client,
            CookiePair(refreshCookie),
            ParseSessionIdentifier(body),
            refreshPath: refreshPath);
        Assert.Equal(HttpStatusCode.Forbidden, refresh.StatusCode);
        _ = ParseChallenge(refresh, DbscConstants.Headers.Challenge);
    }

    [Fact]
    public async Task EndpointMatching_IsCaseInsensitive_ButTrailingSlashIsSignificant()
    {
        using var host = await CreateHostAsync(
            registrationPath: CustomRegistrationPath,
            refreshPath: CustomRefreshPath);
        var client = host.GetTestServer().CreateClient();

        var caseVariant = await client.PostAsync("/CUSTOM/DBSC/REGISTER", content: null);
        var trailingSlashVariant = await client.PostAsync(CustomRegistrationPath + "/", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, caseVariant.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, trailingSlashVariant.StatusCode);
    }

    private static string Serialize(SessionInstruction instruction)
        => JsonSerializer.Serialize(instruction, DbscJsonContext.Default.SessionInstruction);

    private static async Task<string> SignInRegisterAndReadBodyAsync(
        HttpClient client,
        string pathBase = "",
        string registrationPath = RegistrationPath)
    {
        var response = await SignInAndRegisterAsync(client, DbscProofKey.CreateEs256(), pathBase, registrationPath);
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<HttpResponseMessage> SignInAndRegisterAsync(
        HttpClient client,
        DbscProofKey proofKey,
        string pathBase = "",
        string registrationPath = RegistrationPath)
    {
        var signIn = await client.GetAsync(GetRequestTarget(pathBase, "/signin"));
        signIn.EnsureSuccessStatusCode();

        return await RegisterAsync(client, signIn, proofKey, pathBase, registrationPath);
    }

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        HttpResponseMessage signIn,
        DbscProofKey proofKey,
        string pathBase = "",
        string registrationPath = RegistrationPath)
    {
        var sourceCookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(SourceCookieName + "=", StringComparison.Ordinal));
        var challenge = ParseChallenge(signIn, DbscConstants.Headers.Registration);

        var proof = proofKey.CreateProof(challenge);
        var register = new HttpRequestMessage(HttpMethod.Post, GetRequestTarget(pathBase, registrationPath));
        register.Headers.TryAddWithoutValidation("Cookie", CookiePair(sourceCookie));
        register.Headers.TryAddWithoutValidation(DbscConstants.Headers.Proof, proof);
        var response = await client.SendAsync(register);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static async Task<HttpResponseMessage> SendRefreshAsync(
        HttpClient client,
        string refreshCookie,
        string sessionId,
        string? proof = null,
        string pathBase = "",
        string refreshPath = RefreshPath)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GetRequestTarget(pathBase, refreshPath));
        request.Headers.TryAddWithoutValidation("Cookie", refreshCookie);
        request.Headers.TryAddWithoutValidation(DbscConstants.Headers.SessionId, sessionId);
        if (proof is not null)
        {
            request.Headers.TryAddWithoutValidation(DbscConstants.Headers.Proof, proof);
        }

        return await client.SendAsync(request);
    }

    private static async Task<IHost> CreateHostAsync(
        Action<DbscOptions>? configureDbsc = null,
        string? pathBase = null,
        string registrationPath = RegistrationPath,
        string refreshPath = RefreshPath)
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
                        builder.AddDbsc(DbscDefaults.AuthenticationScheme, options =>
                        {
                            options.SourceScheme = SourceScheme;
                            options.RegistrationPath = new PathString(registrationPath);
                            options.RefreshPath = new PathString(refreshPath);
                            configureDbsc?.Invoke(options);
                        });
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

    private static string GetRequestTarget(string pathBase, string path)
        => new PathString(pathBase).Add(new PathString(path)).ToUriComponent();

    private static string CookiePair(string setCookie)
    {
        var semicolon = setCookie.IndexOf(';');
        return semicolon < 0 ? setCookie : setCookie[..semicolon];
    }

    private static string ParseCookiePath(string setCookie)
        => SetCookieHeaderValue.Parse(setCookie).Path.Value!;

    private static string ParseChallenge(HttpResponseMessage response, string headerName)
    {
        var header = Assert.Single(response.Headers.GetValues(headerName));
        var pattern = headerName == DbscConstants.Headers.Challenge
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
