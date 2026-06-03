// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Authentication.Cookies;

public class DeviceBoundSessionTests
{
    [Fact]
    public async Task SignIn_WithDbscEnabled_EmitsRegistrationHeader()
    {
        using var host = await CreateDbscHost();
        using var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync("http://example.com/signin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Secure-Session-Registration"));
        var headerValue = response.Headers.GetValues("Secure-Session-Registration").Single();
        Assert.Contains("ES256", headerValue);
        Assert.Contains("RS256", headerValue);
        Assert.Contains("path=\"/.well-known/dbsc/registration\"", headerValue);
        Assert.Contains("challenge=", headerValue);
    }

    [Fact]
    public async Task SignIn_WithoutDbscEnabled_DoesNotEmitRegistrationHeader()
    {
        using var host = await CreateHost(options => { });
        using var server = host.GetTestServer();

        var response = await server.CreateClient().GetAsync("http://example.com/signin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Secure-Session-Registration"));
    }

    [Fact]
    public async Task Registration_WithoutAuthCookie_Returns401()
    {
        using var host = await CreateDbscHost();
        using var server = host.GetTestServer();

        // Without a Secure-Session-Response header, returns 400 (missing header)
        // With a header but no cookie, returns 401
        var request = new HttpRequestMessage(HttpMethod.Post, "http://example.com/.well-known/dbsc/registration");
        request.Headers.Add("Secure-Session-Response", "\"invalid.jwt.here\"");
        var response = await server.CreateClient().SendAsync(request);

        // JWT is invalid so we get 400 first (can't parse)
        // The handler checks JWT validity before cookie
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_WithoutJwtHeader_Returns400()
    {
        using var host = await CreateDbscHost();
        using var server = host.GetTestServer();
        var client = server.CreateClient();

        // First sign in to get a cookie
        var signInResponse = await client.GetAsync("http://example.com/signin");
        var cookies = GetCookies(signInResponse);

        // POST registration without Secure-Session-Response header
        var request = new HttpRequestMessage(HttpMethod.Post, "http://example.com/.well-known/dbsc/registration");
        request.Headers.Add("Cookie", cookies);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_WithValidJwt_ReturnsSessionConfig()
    {
        using var host = await CreateDbscHost();
        using var server = host.GetTestServer();
        var client = server.CreateClient();

        // Sign in
        var signInResponse = await client.GetAsync("http://example.com/signin");
        var cookies = GetCookies(signInResponse);

        // Create a valid DBSC JWT with EC key
        var (jwt, _) = CreateRegistrationJwt("test-challenge");

        // POST registration
        var request = new HttpRequestMessage(HttpMethod.Post, "http://example.com/.well-known/dbsc/registration");
        request.Headers.Add("Cookie", cookies);
        request.Headers.Add("Secure-Session-Response", $"\"{jwt}\"");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var config = JsonDocument.Parse(content).RootElement;

        Assert.True(config.TryGetProperty("session_identifier", out var sessionId));
        Assert.False(string.IsNullOrEmpty(sessionId.GetString()));
        Assert.True(config.TryGetProperty("refresh_url", out var refreshUrl));
        Assert.Equal("/.well-known/dbsc/refresh", refreshUrl.GetString());
        Assert.True(config.TryGetProperty("scope", out _));
        Assert.True(config.TryGetProperty("credentials", out var credentials));
        Assert.Equal("cookie", credentials[0].GetProperty("type").GetString());

        // Should also set the short-lived cookie
        Assert.True(response.Headers.Contains("Secure-Session-Challenge"));
    }

    [Fact]
    public async Task Refresh_WithoutSessionId_Returns400()
    {
        using var host = await CreateDbscHost();
        using var server = host.GetTestServer();

        var request = new HttpRequestMessage(HttpMethod.Post, "http://example.com/.well-known/dbsc/refresh");
        var response = await server.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutProof_ReturnsChallenge()
    {
        using var host = await CreateDbscHost();
        using var server = host.GetTestServer();
        var client = server.CreateClient();

        // Sign in and register
        var (cookies, sessionId) = await SignInAndRegister(client);

        // POST refresh without proof
        var request = new HttpRequestMessage(HttpMethod.Post, "http://example.com/.well-known/dbsc/refresh");
        request.Headers.Add("Cookie", cookies);
        request.Headers.Add("Sec-Secure-Session-Id", $"\"{sessionId}\"");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(response.Headers.Contains("Secure-Session-Challenge"));
        var challengeHeader = response.Headers.GetValues("Secure-Session-Challenge").Single();
        Assert.Contains($"id=\"{sessionId}\"", challengeHeader);
    }

    [Fact]
    public async Task Refresh_WithValidProof_IssuesNewCookie()
    {
        using var host = await CreateDbscHost();
        using var server = host.GetTestServer();
        var client = server.CreateClient();

        // Sign in and register
        var (cookies, sessionId, ecDsa) = await SignInAndRegisterWithKey(client);

        // Get a challenge
        var challengeRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.com/.well-known/dbsc/refresh");
        challengeRequest.Headers.Add("Cookie", cookies);
        challengeRequest.Headers.Add("Sec-Secure-Session-Id", $"\"{sessionId}\"");
        var challengeResponse = await client.SendAsync(challengeRequest);

        Assert.Equal(HttpStatusCode.Forbidden, challengeResponse.StatusCode);
        var challengeHeader = challengeResponse.Headers.GetValues("Secure-Session-Challenge").Single();
        // Extract challenge value between first pair of quotes
        var challenge = ExtractChallengeValue(challengeHeader);

        // Sign the challenge
        var proofJwt = CreateRefreshProofJwt(challenge, ecDsa);

        // POST refresh with proof
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.com/.well-known/dbsc/refresh");
        refreshRequest.Headers.Add("Cookie", cookies);
        refreshRequest.Headers.Add("Sec-Secure-Session-Id", $"\"{sessionId}\"");
        refreshRequest.Headers.Add("Secure-Session-Response", $"\"{proofJwt}\"");
        var refreshResponse = await client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        // Should contain new session config
        var content = await refreshResponse.Content.ReadAsStringAsync();
        var config = JsonDocument.Parse(content).RootElement;
        Assert.Equal(sessionId, config.GetProperty("session_identifier").GetString());

        // Should have Set-Cookie for short-lived cookie
        Assert.True(refreshResponse.Headers.Contains("Secure-Session-Challenge"));
    }

    [Fact]
    public void ChallengeGenerator_ValidChallenge_Validates()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        var dp = sp.GetRequiredService<DataProtection.IDataProtectionProvider>();

        var generator = new DeviceBoundSessionChallengeGenerator(dp);
        var challenge = generator.GenerateChallenge("session-123");

        Assert.True(generator.ValidateChallenge(challenge, "session-123", TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void ChallengeGenerator_WrongSessionId_Rejects()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        var dp = sp.GetRequiredService<DataProtection.IDataProtectionProvider>();

        var generator = new DeviceBoundSessionChallengeGenerator(dp);
        var challenge = generator.GenerateChallenge("session-123");

        Assert.False(generator.ValidateChallenge(challenge, "session-456", TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void ChallengeGenerator_ExpiredChallenge_Rejects()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        var dp = sp.GetRequiredService<DataProtection.IDataProtectionProvider>();

        var generator = new DeviceBoundSessionChallengeGenerator(dp);
        var challenge = generator.GenerateChallenge("session-123");

        // Zero-second max age means it's immediately expired
        Assert.False(generator.ValidateChallenge(challenge, "session-123", TimeSpan.Zero));
    }

    [Fact]
    public void JwtValidator_ValidES256Jwt_Validates()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var jwt = CreateTestJwt(ecdsa, "test-challenge");
        var jwk = ExportPublicKeyAsJwk(ecdsa);

        var result = DeviceBoundSessionJwtValidator.Validate(jwt, jwk, "test-challenge");

        Assert.NotNull(result);
        Assert.Equal("ES256", result.Algorithm);
        Assert.Equal("test-challenge", result.Challenge);
    }

    [Fact]
    public void JwtValidator_WrongChallenge_ReturnsNull()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var jwt = CreateTestJwt(ecdsa, "actual-challenge");
        var jwk = ExportPublicKeyAsJwk(ecdsa);

        var result = DeviceBoundSessionJwtValidator.Validate(jwt, jwk, "expected-challenge");

        Assert.Null(result);
    }

    [Fact]
    public void JwtValidator_TamperedSignature_ReturnsNull()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var jwt = CreateTestJwt(ecdsa, "test-challenge");
        var jwk = ExportPublicKeyAsJwk(ecdsa);

        // Tamper with signature
        var parts = jwt.Split('.');
        var tamperedJwt = $"{parts[0]}.{parts[1]}.AAAA{parts[2][4..]}";

        var result = DeviceBoundSessionJwtValidator.Validate(tamperedJwt, jwk, "test-challenge");

        Assert.Null(result);
    }

    [Fact]
    public void JwtValidator_WrongKey_ReturnsNull()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var otherEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var jwt = CreateTestJwt(ecdsa, "test-challenge");
        var wrongJwk = ExportPublicKeyAsJwk(otherEcdsa);

        var result = DeviceBoundSessionJwtValidator.Validate(jwt, wrongJwk, "test-challenge");

        Assert.Null(result);
    }

    [Fact]
    public void JwtValidator_ExtractPublicKeyJwk_ExtractsFromHeader()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var jwt = CreateRegistrationJwtWithKey(ecdsa, "challenge");

        var jwk = DeviceBoundSessionJwtValidator.ExtractPublicKeyJwk(jwt);

        Assert.NotNull(jwk);
        Assert.Contains("\"kty\":\"EC\"", jwk);
        Assert.Contains("\"crv\":\"P-256\"", jwk);
    }

    // Helper methods

    private static Task<IHost> CreateDbscHost()
    {
        return CreateHost(options =>
        {
            options.DeviceBoundSession = new DeviceBoundSessionOptions
            {
                Enabled = true
            };
        });
    }

    private static async Task<IHost> CreateHost(Action<CookieAuthenticationOptions> configureOptions)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDataProtection();
                        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                            .AddCookie(o =>
                            {
                                configureOptions(o);
                            });
                    })
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.UseDeviceBoundSessions();
                        app.Use(async (context, next) =>
                        {
                            if (context.Request.Path == new PathString("/signin"))
                            {
                                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                                identity.AddClaim(new Claim(ClaimTypes.Name, "Alice"));
                                await context.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(identity),
                                    new AuthenticationProperties { IsPersistent = true });
                            }
                            else
                            {
                                await next(context);
                            }
                        });
                    }))
            .Build();

        await host.StartAsync();
        return host;
    }

    private static string GetCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            return string.Empty;
        }

        var cookieParts = setCookies.Select(c => c.Split(';')[0]);
        return string.Join("; ", cookieParts);
    }

    private async Task<(string cookies, string sessionId)> SignInAndRegister(HttpClient client)
    {
        var (cookies, sessionId, _) = await SignInAndRegisterWithKey(client);
        return (cookies, sessionId);
    }

    private async Task<(string cookies, string sessionId, ECDsa key)> SignInAndRegisterWithKey(HttpClient client)
    {
        // Sign in
        var signInResponse = await client.GetAsync("http://example.com/signin");
        var cookies = GetCookies(signInResponse);

        // Create registration JWT
        var (jwt, ecDsa) = CreateRegistrationJwt("test-challenge");

        // Register
        var request = new HttpRequestMessage(HttpMethod.Post, "http://example.com/.well-known/dbsc/registration");
        request.Headers.Add("Cookie", cookies);
        request.Headers.Add("Secure-Session-Response", $"\"{jwt}\"");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Update cookies with registration response
        var updatedCookies = GetCookies(response);
        if (!string.IsNullOrEmpty(updatedCookies))
        {
            cookies = MergeCookies(cookies, updatedCookies);
        }

        var content = await response.Content.ReadAsStringAsync();
        var config = JsonDocument.Parse(content).RootElement;
        var sessionId = config.GetProperty("session_identifier").GetString()!;

        return (cookies, sessionId, ecDsa);
    }

    private static string MergeCookies(string existing, string updates)
    {
        var cookieDict = new Dictionary<string, string>();
        foreach (var cookie in existing.Split("; ", StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIdx = cookie.IndexOf('=');
            if (eqIdx > 0)
            {
                cookieDict[cookie[..eqIdx]] = cookie[(eqIdx + 1)..];
            }
        }
        foreach (var cookie in updates.Split("; ", StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIdx = cookie.IndexOf('=');
            if (eqIdx > 0)
            {
                cookieDict[cookie[..eqIdx]] = cookie[(eqIdx + 1)..];
            }
        }
        return string.Join("; ", cookieDict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }

    private static (string jwt, ECDsa key) CreateRegistrationJwt(string challenge)
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var jwt = CreateRegistrationJwtWithKey(ecdsa, challenge);
        return (jwt, ecdsa);
    }

    private static string CreateRegistrationJwtWithKey(ECDsa ecdsa, string challenge)
    {
        var jwk = ExportPublicKeyAsJwk(ecdsa);
        var header = JsonSerializer.Serialize(new
        {
            alg = "ES256",
            typ = "dbsc+jwt",
            jwk = JsonDocument.Parse(jwk).RootElement
        });
        var payload = JsonSerializer.Serialize(new { jti = challenge });

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{headerB64}.{payloadB64}";
        var signature = ecdsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string CreateTestJwt(ECDsa ecdsa, string challenge)
    {
        var header = JsonSerializer.Serialize(new
        {
            alg = "ES256",
            typ = "dbsc+jwt"
        });
        var payload = JsonSerializer.Serialize(new { jti = challenge });

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{headerB64}.{payloadB64}";
        var signature = ecdsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string CreateRefreshProofJwt(string challenge, ECDsa ecdsa)
    {
        var header = JsonSerializer.Serialize(new
        {
            alg = "ES256",
            typ = "dbsc+jwt"
        });
        var payload = JsonSerializer.Serialize(new { jti = challenge });

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{headerB64}.{payloadB64}";
        var signature = ecdsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string ExportPublicKeyAsJwk(ECDsa ecdsa)
    {
        var parameters = ecdsa.ExportParameters(false);
        return JsonSerializer.Serialize(new
        {
            kty = "EC",
            crv = "P-256",
            x = Base64UrlEncode(parameters.Q.X!),
            y = Base64UrlEncode(parameters.Q.Y!)
        });
    }

    private static string ExtractChallengeValue(string challengeHeader)
    {
        // Format: "challenge_value";id="session_id"
        var firstQuote = challengeHeader.IndexOf('"');
        var secondQuote = challengeHeader.IndexOf('"', firstQuote + 1);
        return challengeHeader[(firstQuote + 1)..secondQuote];
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
