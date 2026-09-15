// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using JsonWebTokenClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;
using JwtSecurityTokenClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

[Collection(nameof(OpenIdConnectMaxAgeTests))]
public class OpenIdConnectMaxAgeTests
{
    private const long Now = 1_700_000_000;
    private const string DisableMaxAgeValidationSwitch = "Microsoft.AspNetCore.Authentication.OpenIdConnect.DisableMaxAgeValidation";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingAuthTimeFailsWhenMaxAgeWasSent(bool useSecurityTokenValidator)
    {
        using var host = await CreateHostAsync(
            useSecurityTokenValidator,
            options => options.MaxAge = TimeSpan.FromMinutes(5),
            new Dictionary<string, object?> { ["front"] = null });

        var transaction = await AuthenticateAsync(host, "front");

        Assert.Equal(HttpStatusCode.BadRequest, transaction.Response.StatusCode);
    }

    [Theory]
    [InlineData(0, HttpStatusCode.Found)]
    [InlineData(-300, HttpStatusCode.Found)]
    [InlineData(-600, HttpStatusCode.Found)]
    [InlineData(-601, HttpStatusCode.BadRequest)]
    [InlineData(300, HttpStatusCode.Found)]
    [InlineData(301, HttpStatusCode.BadRequest)]
    public async Task ValidatesAgeAndDefaultClockSkewBoundaries(long authTimeOffset, HttpStatusCode expectedStatus)
    {
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options => options.MaxAge = TimeSpan.FromMinutes(5),
            new Dictionary<string, object?> { ["front"] = Now + authTimeOffset });

        var transaction = await AuthenticateAsync(host, "front");

        Assert.True(
            transaction.Response.StatusCode == expectedStatus,
            transaction.Response.Headers.TryGetValues("X-Error", out var errors) ? string.Join(Environment.NewLine, errors) : transaction.Response.StatusCode.ToString());
    }

    [Theory]
    [InlineData(-360, HttpStatusCode.Found)]
    [InlineData(-361, HttpStatusCode.BadRequest)]
    [InlineData(60, HttpStatusCode.Found)]
    [InlineData(61, HttpStatusCode.BadRequest)]
    public async Task UsesConfiguredClockSkew(long authTimeOffset, HttpStatusCode expectedStatus)
    {
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options =>
            {
                options.MaxAge = TimeSpan.FromMinutes(5);
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(1);
            },
            new Dictionary<string, object?> { ["front"] = Now + authTimeOffset });

        var transaction = await AuthenticateAsync(host, "front");

        Assert.Equal(expectedStatus, transaction.Response.StatusCode);
    }

    public static TheoryData<object?> InvalidAuthTimes => new()
    {
        null,
        "1700000000",
        1_700_000_000.5,
        -1L,
        ulong.MaxValue,
        long.MaxValue,
    };

    [Theory]
    [MemberData(nameof(InvalidAuthTimes))]
    public async Task RejectsInvalidNumericDates(object? authTime)
    {
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options => options.MaxAge = TimeSpan.FromMinutes(5),
            new Dictionary<string, object?> { ["front"] = authTime });

        var transaction = await AuthenticateAsync(host, "front");

        Assert.Equal(HttpStatusCode.BadRequest, transaction.Response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DoesNotRequireAuthTimeWhenMaxAgeIsUnset(bool useSecurityTokenValidator)
    {
        using var host = await CreateHostAsync(
            useSecurityTokenValidator,
            _ => { },
            new Dictionary<string, object?> { ["front"] = null });

        var transaction = await AuthenticateAsync(host, "front", expectedMaxAge: null);

        Assert.Equal(HttpStatusCode.Found, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task PerChallengeMaxAgeOverridesOptionsAndIsCorrelatedPerTransaction()
    {
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options =>
            {
                options.MaxAge = TimeSpan.FromMinutes(10);
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
            },
            new Dictionary<string, object?>
            {
                ["fresh-enough-for-five"] = Now - 400,
                ["fresh-enough-for-ten"] = Now - 400,
            });

        var fiveMinuteChallenge = await ChallengeAsync(host, TimeSpan.FromMinutes(5));
        var tenMinuteChallenge = await ChallengeAsync(host);

        Assert.Equal("300", fiveMinuteChallenge.MaxAge);
        Assert.Equal("600", tenMinuteChallenge.MaxAge);

        var fiveMinuteResponse = await CallbackAsync(host, fiveMinuteChallenge, "fresh-enough-for-five");
        var tenMinuteResponse = await CallbackAsync(host, tenMinuteChallenge, "fresh-enough-for-ten");

        Assert.Equal(HttpStatusCode.BadRequest, fiveMinuteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Found, tenMinuteResponse.StatusCode);
    }

    [Theory]
    [InlineData("id_token", false)]
    [InlineData("id_token", true)]
    [InlineData("id_token token", false)]
    [InlineData("id_token token", true)]
    [InlineData("code", false)]
    [InlineData("code", true)]
    [InlineData("code token", false)]
    [InlineData("code token", true)]
    [InlineData("code id_token", false)]
    [InlineData("code id_token", true)]
    [InlineData("code id_token token", false)]
    [InlineData("code id_token token", true)]
    public async Task ValidatesAllSupportedAuthorizationFlowTokens(string responseType, bool useSecurityTokenValidator)
    {
        var authTimes = new Dictionary<string, object?> { ["front"] = Now };
        if (responseType.Contains("code", StringComparison.Ordinal))
        {
            authTimes["back"] = responseType.Contains("id_token", StringComparison.Ordinal) ? Now - 601 : null;
        }

        using var host = await CreateHostAsync(
            useSecurityTokenValidator,
            options =>
            {
                options.MaxAge = TimeSpan.FromMinutes(5);
                options.ResponseType = responseType;
            },
            authTimes);

        var transaction = await AuthenticateAsync(
            host,
            responseType.Contains("id_token", StringComparison.Ordinal) ? "front" : null,
            includeCode: responseType.Contains("code", StringComparison.Ordinal),
            includeAccessToken: responseType.Split(' ').Contains("token", StringComparer.Ordinal));

        Assert.Equal(
            !responseType.Contains("code", StringComparison.Ordinal) ? HttpStatusCode.Found : HttpStatusCode.BadRequest,
            transaction.Response.StatusCode);
    }

    [Fact]
    public async Task HybridFlowValidatesFrontChannelTokenBeforeRedeemingCode()
    {
        var backchannelCalls = 0;
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options =>
            {
                options.MaxAge = TimeSpan.FromMinutes(5);
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
            },
            new Dictionary<string, object?>
            {
                ["front"] = Now - 601,
                ["back"] = Now,
            },
            onBackchannelCall: () => backchannelCalls++);

        var transaction = await AuthenticateAsync(host, "front", includeCode: true);

        Assert.Equal(HttpStatusCode.BadRequest, transaction.Response.StatusCode);
        Assert.Equal(0, backchannelCalls);
    }

    [Fact]
    public async Task FailureStopsTicketAndUserInfoAndUsesExistingFailureEvents()
    {
        var events = new List<string>();
        var backchannelCalls = 0;
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options =>
            {
                options.MaxAge = TimeSpan.FromMinutes(5);
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Events.OnTokenValidated = context =>
                {
                    events.Add("TokenValidated");
                    return Task.CompletedTask;
                };
                options.Events.OnAuthenticationFailed = context =>
                {
                    events.Add("AuthenticationFailed");
                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = context =>
                {
                    events.Add("RemoteFailure");
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return Task.CompletedTask;
                };
                options.Events.OnTicketReceived = context =>
                {
                    events.Add("TicketReceived");
                    return Task.CompletedTask;
                };
            },
            new Dictionary<string, object?> { ["front"] = null },
            onBackchannelCall: () => backchannelCalls++);

        var transaction = await AuthenticateAsync(host, "front");

        Assert.Equal(HttpStatusCode.BadRequest, transaction.Response.StatusCode);
        Assert.Equal(["TokenValidated", "AuthenticationFailed", "RemoteFailure"], events);
        Assert.Equal(0, backchannelCalls);
        Assert.DoesNotContain(
            transaction.Response.Headers,
            header => header.Key == "Set-Cookie" && header.Value.Any(value => value.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task CompatibilitySwitchTrueRestoresLegacyRequestOnlyBehavior()
    {
        AppContext.SetSwitch(DisableMaxAgeValidationSwitch, true);
        try
        {
            using var host = await CreateHostAsync(
                useSecurityTokenValidator: false,
                options => options.MaxAge = TimeSpan.FromMinutes(5),
                new Dictionary<string, object?> { ["front"] = null });

            var transaction = await AuthenticateAsync(host, "front");

            Assert.Equal(HttpStatusCode.Found, transaction.Response.StatusCode);
        }
        finally
        {
            AppContext.SetSwitch(DisableMaxAgeValidationSwitch, false);
        }
    }

    [Fact]
    public async Task CompatibilitySwitchFalseValidates()
    {
        AppContext.SetSwitch(DisableMaxAgeValidationSwitch, false);
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options => options.MaxAge = TimeSpan.FromMinutes(5),
            new Dictionary<string, object?> { ["front"] = null });

        var transaction = await AuthenticateAsync(host, "front");

        Assert.Equal(HttpStatusCode.BadRequest, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task MaxAgeCannotBeChangedByPushAuthorizationEvent()
    {
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options =>
            {
                options.MaxAge = TimeSpan.FromMinutes(5);
                options.Configuration!.PushedAuthorizationRequestEndpoint = "https://idp.example/par";
                options.Events.OnPushAuthorization = context =>
                {
                    context.ProtocolMessage.MaxAge = "301";
                    return Task.CompletedTask;
                };
            },
            new Dictionary<string, object?>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.GetTestServer().CreateClient().GetAsync("/challenge"));

        Assert.Equal(
            "The max_age parameter cannot be changed in OnPushAuthorization. Change it in OnRedirectToIdentityProvider so that the value can be correlated with the authorization response.",
            exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PushAuthorizationMutationIsAllowedWhenNotUsedOrValidationIsDisabled(bool skipPush)
    {
        AppContext.SetSwitch(DisableMaxAgeValidationSwitch, !skipPush);
        try
        {
            using var host = await CreateHostAsync(
                useSecurityTokenValidator: false,
                options =>
                {
                    options.MaxAge = TimeSpan.FromMinutes(5);
                    options.Configuration!.PushedAuthorizationRequestEndpoint = "https://idp.example/par";
                    options.Events.OnPushAuthorization = context =>
                    {
                        context.ProtocolMessage.MaxAge = "301";
                        if (skipPush)
                        {
                            context.SkipPush();
                        }
                        else
                        {
                            context.HandlePush("urn:request");
                        }
                        return Task.CompletedTask;
                    };
                },
                new Dictionary<string, object?>());

            var response = await host.GetTestServer().CreateClient().GetAsync("/challenge");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
        finally
        {
            AppContext.SetSwitch(DisableMaxAgeValidationSwitch, false);
        }
    }

    [Fact]
    public async Task CorrelatesMaxAgeChangedByRedirectEvent()
    {
        using var host = await CreateHostAsync(
            useSecurityTokenValidator: false,
            options =>
            {
                options.MaxAge = TimeSpan.FromMinutes(10);
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.MaxAge = "300";
                    return Task.CompletedTask;
                };
            },
            new Dictionary<string, object?> { ["front"] = Now - 301 });

        var transaction = await AuthenticateAsync(host, "front");

        Assert.Equal("300", transaction.Challenge.MaxAge);
        Assert.Equal(HttpStatusCode.BadRequest, transaction.Response.StatusCode);
    }

    private static async Task<IHost> CreateHostAsync(
        bool useSecurityTokenValidator,
        Action<OpenIdConnectOptions> configure,
        IReadOnlyDictionary<string, object?> tokenAuthTimes,
        Action? onBackchannelCall = null)
    {
        var stateDataFormat = new TestStateDataFormat();
        var tokenFactory = new TestTokenFactory(tokenAuthTimes);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(Now));

        var host = new HostBuilder()
            .ConfigureWebHost(builder => builder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services
                        .AddAuthentication(options =>
                        {
                            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                        })
                        .AddCookie()
                        .AddOpenIdConnect(options =>
                        {
                            options.ClientId = "client";
                            options.ClientSecret = "secret";
                            options.Configuration = new OpenIdConnectConfiguration
                            {
                                AuthorizationEndpoint = "https://idp.example/authorize",
                                TokenEndpoint = "https://idp.example/token",
                                UserInfoEndpoint = "https://idp.example/userinfo",
                            };
                            options.ProtocolValidator = new TestProtocolValidator
                            {
                                RequireNonce = false,
                            };
                            options.StateDataFormat = stateDataFormat;
                            options.TimeProvider = timeProvider;
                            options.UsePkce = false;
                            options.UseSecurityTokenValidator = useSecurityTokenValidator;
                            options.BackchannelHttpHandler = new TestBackchannel(onBackchannelCall);

                            if (useSecurityTokenValidator)
                            {
#pragma warning disable CS0618 // Type or member is obsolete
                                options.SecurityTokenValidator = new TestSecurityTokenValidator(tokenFactory);
#pragma warning restore CS0618 // Type or member is obsolete
                            }
                            else
                            {
                                options.TokenHandler = new TestTokenHandler(tokenFactory);
                            }

                            options.Events.OnRemoteFailure = context =>
                            {
                                context.HandleResponse();
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                context.Response.Headers["X-Error"] = context.Failure?.ToString();
                                return Task.CompletedTask;
                            };

                            configure(options);
                        });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(async context =>
                    {
                        if (context.Request.Path == "/challenge")
                        {
                            AuthenticationProperties properties;
                            if (context.Request.Query.TryGetValue("maxAge", out var maxAge))
                            {
                                properties = new OpenIdConnectChallengeProperties
                                {
                                    MaxAge = TimeSpan.FromSeconds(long.Parse(maxAge.ToString(), CultureInfo.InvariantCulture)),
                                    RedirectUri = "/complete",
                                };
                            }
                            else
                            {
                                properties = new AuthenticationProperties { RedirectUri = "/complete" };
                            }

                            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
                        }
                        else
                        {
                            context.Response.StatusCode = StatusCodes.Status204NoContent;
                        }
                    });
                }))
            .Build();

        await host.StartAsync();
        return host;
    }

    private static async Task<Transaction> AuthenticateAsync(
        IHost host,
        string? idToken,
        bool includeCode = false,
        bool includeAccessToken = false,
        string? expectedMaxAge = "300")
    {
        var challenge = await ChallengeAsync(host);
        Assert.Equal(expectedMaxAge, challenge.MaxAge);
        var response = await CallbackAsync(host, challenge, idToken, includeCode, includeAccessToken);
        return new Transaction(challenge, response);
    }

    private static async Task<Challenge> ChallengeAsync(IHost host, TimeSpan? maxAge = null)
    {
        var server = host.GetTestServer();
        var client = server.CreateClient();
        var path = maxAge.HasValue ? $"/challenge?maxAge={maxAge.Value.TotalSeconds}" : "/challenge";
        var response = await client.GetAsync(path);
        var query = QueryHelpers.ParseQuery(response.Headers.Location!.Query);
        var state = query[OpenIdConnectParameterNames.State].ToString();
        var properties = PropertiesSerializer.Default.Deserialize(Base64UrlTextEncoder.Decode(state));
        Assert.NotNull(properties);
        var correlatedMaxAge = properties.Items.TryGetValue(".OpenIdConnect.MaxAge", out var value) ? value : null;
        var requestedMaxAge = query.TryGetValue(OpenIdConnectParameterNames.MaxAge, out var maxAgeValue) ? maxAgeValue.ToString() : null;
        Assert.Equal(requestedMaxAge, correlatedMaxAge);

        var cookies = string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(value => value[..value.IndexOf(';')]));
        return new Challenge(state, cookies, correlatedMaxAge);
    }

    private static async Task<HttpResponseMessage> CallbackAsync(
        IHost host,
        Challenge challenge,
        string? idToken,
        bool includeCode = false,
        bool includeAccessToken = false)
    {
        var parameters = new Dictionary<string, string>
        {
            [OpenIdConnectParameterNames.State] = challenge.State,
        };
        if (idToken is not null)
        {
            parameters[OpenIdConnectParameterNames.IdToken] = idToken;
        }
        if (includeCode)
        {
            parameters[OpenIdConnectParameterNames.Code] = "code";
        }
        if (includeAccessToken)
        {
            parameters[OpenIdConnectParameterNames.AccessToken] = "front-access-token";
        }

        var callback = new HttpRequestMessage(HttpMethod.Post, "/signin-oidc")
        {
            Content = new FormUrlEncodedContent(parameters),
        };
        callback.Headers.Add("Cookie", challenge.Cookies);

        return await host.GetTestServer().CreateClient().SendAsync(callback);
    }

    private sealed record Challenge(string State, string Cookies, string? MaxAge);

    private sealed record Transaction(Challenge Challenge, HttpResponseMessage Response);

    private sealed class TestStateDataFormat : ISecureDataFormat<AuthenticationProperties>
    {
        public string Protect(AuthenticationProperties data) =>
            Base64UrlTextEncoder.Encode(PropertiesSerializer.Default.Serialize(data));

        public string Protect(AuthenticationProperties data, string? purpose) => Protect(data);

        public AuthenticationProperties? Unprotect(string? protectedText) =>
            protectedText is null ? null : PropertiesSerializer.Default.Deserialize(Base64UrlTextEncoder.Decode(protectedText));

        public AuthenticationProperties? Unprotect(string? protectedText, string? purpose) => Unprotect(protectedText);
    }

    private sealed class TestTokenFactory(IReadOnlyDictionary<string, object?> tokenAuthTimes)
    {
        public JwtSecurityToken CreateJwt(string token)
        {
            var payload = new JwtPayload
            {
                [JwtSecurityTokenClaimNames.Sub] = "subject",
            };

            if (tokenAuthTimes[token] is { } authTime)
            {
                payload[JwtSecurityTokenClaimNames.AuthTime] = authTime;
            }

            return new JwtSecurityToken(new JwtHeader(), payload);
        }

        public JsonWebToken CreateJsonWebToken(string token)
        {
            var jwt = CreateJwt(token);
            return new JsonWebToken(jwt.EncodedHeader + "." + jwt.EncodedPayload + ".");
        }
    }

    private sealed class TestTokenHandler(TestTokenFactory tokenFactory) : TokenHandler
    {
        public override Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters) =>
            Task.FromResult(new TokenValidationResult
            {
                ClaimsIdentity = new ClaimsIdentity([new Claim(JsonWebTokenClaimNames.Sub, "subject")], "Test"),
                IsValid = true,
                SecurityToken = tokenFactory.CreateJsonWebToken(token),
            });
    }

    private sealed class TestSecurityTokenValidator(TestTokenFactory tokenFactory) : ISecurityTokenValidator
    {
        public bool CanValidateToken => true;

        public int MaximumTokenSizeInBytes { get; set; }

        public bool CanReadToken(string securityToken) => true;

        public ClaimsPrincipal ValidateToken(
            string securityToken,
            TokenValidationParameters validationParameters,
            out SecurityToken validatedToken)
        {
            validatedToken = tokenFactory.CreateJwt(securityToken);
            return new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtSecurityTokenClaimNames.Sub, "subject")], "Test"));
        }
    }

    [CollectionDefinition(nameof(OpenIdConnectMaxAgeTests), DisableParallelization = true)]
    public class OpenIdConnectMaxAgeTestsCollection;

    private sealed class TestProtocolValidator : OpenIdConnectProtocolValidator
    {
        public override void ValidateAuthenticationResponse(OpenIdConnectProtocolValidationContext validationContext)
        {
        }

        public override void ValidateTokenResponse(OpenIdConnectProtocolValidationContext validationContext)
        {
        }
    }

    private sealed class TestBackchannel(Action? onCall) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onCall?.Invoke();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"access","token_type":"Bearer","id_token":"back"}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        }
    }
}
