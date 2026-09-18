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
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

public class DbscEventsTests
{
    private const string SourceScheme = "Source";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string RefreshCookieName = ".AspNetCore.DBSC.Refresh";
    private const string SessionCookieName = ".AspNetCore.DBSC.Session";
    private const string RegistrationPath = "/.well-known/dbsc/registration";
    private const string RefreshPath = "/.well-known/dbsc/refresh";
    private const string FederationProviderKey = "federation_provider";
    private const string FederationProvider = "contoso";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Events_FireExactlyOnceAtSuccessfulProtocolStages(bool useEventsType)
    {
        var recorder = new EventRecorder();
        using var host = await CreateHostAsync(recorder, useEventsType);
        var client = host.GetTestClient();
        var proofKey = DbscProofKey.CreateEs256();

        using var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        Assert.Equal(1, recorder.RegistrationHeaderCreatingCount);
        Assert.Equal(0, recorder.SessionRegisteredCount);
        Assert.Equal(0, recorder.SessionRefreshedCount);
        Assert.Equal(FederationProvider, recorder.FederationProvider);

        using var registration = await RegisterAsync(client, signIn, proofKey);
        Assert.Equal(1, recorder.RegistrationHeaderCreatingCount);
        Assert.Equal(1, recorder.SessionRegisteredCount);
        Assert.Equal(0, recorder.SessionRefreshedCount);

        var registrationBody = await registration.Content.ReadAsStringAsync();
        var sessionId = ParseSessionIdentifier(registrationBody);
        var refreshCookie = Assert.Single(
            registration.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));

        using var challengeResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponse.StatusCode);
        Assert.Equal(0, recorder.SessionRefreshedCount);

        var challenge = ParseChallenge(challengeResponse, DbscConstants.Headers.Challenge);
        var proof = proofKey.CreateProof(challenge, includeJwkHeader: false);
        using var refreshResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId, proof);
        refreshResponse.EnsureSuccessStatusCode();

        Assert.Equal(1, recorder.RegistrationHeaderCreatingCount);
        Assert.Equal(1, recorder.SessionRegisteredCount);
        Assert.Equal(1, recorder.SessionRefreshedCount);
        Assert.Equal(["registration-header", "registered", "refreshed"], recorder.EventOrder);
    }

    [Fact]
    public async Task DefaultEvents_LeaveRegistrationAndRefreshBehaviorUnchanged()
    {
        using var host = await CreateHostAsync(recorder: null, useEventsType: false);
        var client = host.GetTestClient();
        var proofKey = DbscProofKey.CreateEs256();

        using var signIn = await client.GetAsync("/signin");
        signIn.EnsureSuccessStatusCode();
        _ = Assert.Single(signIn.Headers.GetValues(DbscConstants.Headers.Registration));

        using var registration = await RegisterAsync(client, signIn, proofKey);
        registration.EnsureSuccessStatusCode();
        var body = await registration.Content.ReadAsStringAsync();
        var refreshCookie = Assert.Single(
            registration.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(RefreshCookieName + "=", StringComparison.Ordinal));
        _ = Assert.Single(
            registration.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(SessionCookieName + "=", StringComparison.Ordinal));

        var sessionId = ParseSessionIdentifier(body);
        using var challengeResponse = await SendRefreshAsync(client, CookiePair(refreshCookie), sessionId);
        Assert.Equal(HttpStatusCode.Forbidden, challengeResponse.StatusCode);
        var challenge = ParseChallenge(challengeResponse, DbscConstants.Headers.Challenge);

        using var refreshResponse = await SendRefreshAsync(
            client,
            CookiePair(refreshCookie),
            sessionId,
            proofKey.CreateProof(challenge, includeJwkHeader: false));
        refreshResponse.EnsureSuccessStatusCode();
        _ = Assert.Single(
            refreshResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(SessionCookieName + "=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WriteRegistrationAsync_RaisesRegistrationHeaderCreatingLikeAutomaticSignIn()
    {
        var recorder = new EventRecorder();
        using var host = await CreateHostAsync(recorder, useEventsType: false);
        var client = host.GetTestClient();

        using var direct = await client.GetAsync("/direct");
        direct.EnsureSuccessStatusCode();
        Assert.Equal(1, recorder.RegistrationHeaderCreatingCount);
        _ = Assert.Single(direct.Headers.GetValues(DbscConstants.Headers.Registration));

        using var automatic = await client.GetAsync("/signin");
        automatic.EnsureSuccessStatusCode();
        Assert.Equal(2, recorder.RegistrationHeaderCreatingCount);
        _ = Assert.Single(automatic.Headers.GetValues(DbscConstants.Headers.Registration));

        Assert.Equal(["/direct", "/signin"], recorder.RegistrationHeaderRequestPaths);
        Assert.All(recorder.HeaderWasPresentDuringEvent, Assert.False);
    }

    private static async Task<IHost> CreateHostAsync(EventRecorder? recorder, bool useEventsType)
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
                        if (recorder is not null)
                        {
                            services.AddSingleton(recorder);
                            services.AddScoped<RecordingEvents>();
                        }

                        services.AddAuthentication(SourceScheme)
                            .AddCookie(SourceScheme, options => options.Cookie.Name = SourceCookieName)
                            .AddDbsc(
                                DbscDefaults.AuthenticationScheme,
                                options =>
                                {
                                    options.SourceScheme = SourceScheme;
                                    if (recorder is null)
                                    {
                                        return;
                                    }

                                    if (useEventsType)
                                    {
                                        options.EventsType = typeof(RecordingEvents);
                                    }
                                    else
                                    {
                                        options.Events.OnRegistrationHeaderCreating = recorder.RegistrationHeaderCreating;
                                        options.Events.OnSessionRegistered = recorder.SessionRegistered;
                                        options.Events.OnSessionRefreshed = recorder.SessionRefreshed;
                                    }
                                });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/signin", context =>
                            {
                                var properties = new AuthenticationProperties();
                                properties.Items[FederationProviderKey] = FederationProvider;
                                return context.SignInAsync(SourceScheme, Principal(), properties);
                            });
                            endpoints.MapGet("/direct", async context =>
                            {
                                context.User = Principal();
                                await DbscRegistration.WriteRegistrationAsync(
                                    context,
                                    DbscDefaults.AuthenticationScheme);
                            });
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        HttpResponseMessage signIn,
        DbscProofKey proofKey)
    {
        var sourceCookie = Assert.Single(
            signIn.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(SourceCookieName + "=", StringComparison.Ordinal));
        var challenge = ParseChallenge(signIn, DbscConstants.Headers.Registration);
        var request = new HttpRequestMessage(HttpMethod.Post, RegistrationPath);
        request.Headers.TryAddWithoutValidation("Cookie", CookiePair(sourceCookie));
        request.Headers.TryAddWithoutValidation(DbscConstants.Headers.Proof, proofKey.CreateProof(challenge));

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static Task<HttpResponseMessage> SendRefreshAsync(
        HttpClient client,
        string refreshCookie,
        string sessionId,
        string? proof = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
        request.Headers.TryAddWithoutValidation("Cookie", refreshCookie);
        request.Headers.TryAddWithoutValidation(DbscConstants.Headers.SessionId, sessionId);
        if (proof is not null)
        {
            request.Headers.TryAddWithoutValidation(DbscConstants.Headers.Proof, proof);
        }

        return client.SendAsync(request);
    }

    private static ClaimsPrincipal Principal()
    {
        var identity = new ClaimsIdentity(SourceScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "alice"));
        return new ClaimsPrincipal(identity);
    }

    private static string CookiePair(string setCookie)
    {
        var semicolon = setCookie.IndexOf(';');
        return semicolon < 0 ? setCookie : setCookie[..semicolon];
    }

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

    private static string ParseSessionIdentifier(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("session_identifier").GetString()!;
    }

    private sealed class RecordingEvents : DbscEvents
    {
        private readonly EventRecorder _recorder;

        public RecordingEvents(EventRecorder recorder)
        {
            _recorder = recorder;
        }

        public override Task RegistrationHeaderCreating(DbscRegistrationHeaderCreatingContext context)
            => _recorder.RegistrationHeaderCreating(context);

        public override Task SessionRegistered(DbscRegisteredContext context)
            => _recorder.SessionRegistered(context);

        public override Task SessionRefreshed(DbscRefreshedContext context)
            => _recorder.SessionRefreshed(context);
    }

    private sealed class EventRecorder
    {
        public int RegistrationHeaderCreatingCount { get; private set; }

        public int SessionRegisteredCount { get; private set; }

        public int SessionRefreshedCount { get; private set; }

        public string? FederationProvider { get; private set; }

        public List<string> EventOrder { get; } = [];

        public List<string> RegistrationHeaderRequestPaths { get; } = [];

        public List<bool> HeaderWasPresentDuringEvent { get; } = [];

        public Task RegistrationHeaderCreating(DbscRegistrationHeaderCreatingContext context)
        {
            RegistrationHeaderCreatingCount++;
            EventOrder.Add("registration-header");
            RegistrationHeaderRequestPaths.Add(context.Request.Path);
            HeaderWasPresentDuringEvent.Add(context.Response.Headers.ContainsKey(DbscConstants.Headers.Registration));
            context.Properties.Items.TryGetValue(FederationProviderKey, out var provider);
            FederationProvider = provider;
            return Task.CompletedTask;
        }

        public Task SessionRegistered(DbscRegisteredContext context)
        {
            SessionRegisteredCount++;
            EventOrder.Add("registered");
            return Task.CompletedTask;
        }

        public Task SessionRefreshed(DbscRefreshedContext context)
        {
            SessionRefreshedCount++;
            EventOrder.Add("refreshed");
            return Task.CompletedTask;
        }
    }
}
