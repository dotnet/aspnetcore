// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0030 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionInstructionTests
{
    private const string SourceScheme = "Source";
    private const string SourceCookieName = ".AspNetCore.Source";
    private const string RegistrationPath = "/.well-known/dbsc/registration";

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
        var sourceCookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"),
            v => v.StartsWith(SourceCookieName + "=", StringComparison.Ordinal));
        var challenge = ParseChallenge(signIn);
        var proof = DbscProofKey.CreateEs256().CreateProof(challenge);
        var register = new HttpRequestMessage(HttpMethod.Post, "/foo" + RegistrationPath);
        register.Headers.TryAddWithoutValidation("Cookie", CookiePair(sourceCookie));
        register.Headers.TryAddWithoutValidation(DeviceBoundSessionConstants.Headers.Proof, proof);
        var response = await client.SendAsync(register);
        response.EnsureSuccessStatusCode();
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
        return await response.Content.ReadAsStringAsync();
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

    private static string ParseChallenge(HttpResponseMessage response)
    {
        var header = Assert.Single(response.Headers.GetValues(DeviceBoundSessionConstants.Headers.Registration));
        var match = Regex.Match(header, "challenge=\"([^\"]+)\"");
        Assert.True(match.Success, $"No challenge found in registration header: {header}");
        return match.Groups[1].Value;
    }
}
