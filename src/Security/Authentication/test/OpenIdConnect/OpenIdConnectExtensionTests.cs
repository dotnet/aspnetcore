// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

/// <summary>
/// Tests for the OpenIdConnectHandler extension points:
/// <see cref="OpenIdConnectHandler.ResolveClientRegistrationAsync"/>,
/// and <see cref="AuthorizationCodeReceivedContext.HandleClientAuthentication"/>.
/// </summary>
public class OpenIdConnectExtensionTests
{
    private static readonly string ChallengeEndpoint = TestServerBuilder.TestHost + TestServerBuilder.Challenge;

    // ─────────────────────────────────────────────────────────────────────
    // ResolveClientRegistrationAsync — ClientId tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveClientRegistration_DefaultReturnsOptionsClientId()
    {
        string? capturedClientId = null;

        var server = CreateServerWithEvents(options =>
        {
            options.ClientId = "default-client-id";
            options.Events.OnRedirectToIdentityProvider = ctx =>
            {
                capturedClientId = ctx.ProtocolMessage.ClientId;
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };
        });

        var transaction = await server.SendAsync(ChallengeEndpoint);

        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Equal("default-client-id", capturedClientId);
    }

    [Fact]
    public async Task ResolveClientRegistration_OverrideClientIdUsedInChallengeRequest()
    {
        string? capturedClientId = null;

        var server = CreateServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.ClientId = "default-client-id";
            options.Events.OnRedirectToIdentityProvider = ctx =>
            {
                capturedClientId = ctx.ProtocolMessage.ClientId;
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };
        });

        var transaction = await server.SendAsync(ChallengeEndpoint);

        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Equal("dynamic-client-id", capturedClientId);
    }

    [Fact]
    public async Task ResolveClientRegistration_OverrideClientIdUsedInTokenEndpointRequest()
    {
        string? capturedTokenRequestClientId = null;

        var server = CreateCodeFlowServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.Events.OnAuthorizationCodeReceived = ctx =>
            {
                capturedTokenRequestClientId = ctx.TokenEndpointRequest?.ClientId;
                ctx.HandleCodeRedemption("test_access_token", "my_id_token");
                return Task.CompletedTask;
            };
        });

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.Equal("dynamic-client-id", capturedTokenRequestClientId);
    }

    [Fact]
    public async Task ResolveClientRegistration_OverrideClientIdUsedInProtocolValidation()
    {
        string? validationClientId = null;

        var server = CreateCodeFlowServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.ProtocolValidator = new CapturingProtocolValidator(clientId => validationClientId = clientId);
        });

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.Equal("dynamic-client-id", validationClientId);
    }

    [Fact]
    public async Task ResolveClientRegistration_OverrideClientIdUsedInPushedAuthorizationRequest()
    {
        var mockBackchannel = new ParCapturingBackchannel();
        string? capturedPARClientId = null;

        var server = CreateServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.ClientId = "default-client-id";
            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Require;
            options.Configuration = new OpenIdConnectConfiguration
            {
                AuthorizationEndpoint = "https://testauthority/authorize",
                PushedAuthorizationRequestEndpoint = "https://testauthority/par"
            };
            options.Events.OnRedirectToIdentityProvider = ctx =>
            {
                capturedPARClientId = ctx.ProtocolMessage.GetParameter("client_id");
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };
        }, mockBackchannel);

        var transaction = await server.SendAsync(ChallengeEndpoint);

        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Equal("dynamic-client-id", capturedPARClientId);
        Assert.Equal("dynamic-client-id", mockBackchannel.PushedParameters["client_id"]);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ResolveClientRegistrationAsync — ClientSecret tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveClientRegistration_OverrideClientSecretUsedInTokenEndpointRequest()
    {
        IDictionary<string, string>? capturedTokenParams = null;

        var backchannel = new TokenCapturingBackchannel(parameters => capturedTokenParams = parameters);
        var server = CreateCodeFlowServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.ClientSecret = "default-secret";
        }, backchannel: backchannel);

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.NotNull(capturedTokenParams);
        Assert.Equal("dynamic-secret", capturedTokenParams!["client_secret"]);
    }

    [Fact]
    public async Task ResolveClientRegistration_NullClientSecretOmitsSecretFromTokenRequest()
    {
        IDictionary<string, string>? capturedTokenParams = null;

        var backchannel = new TokenCapturingBackchannel(parameters => capturedTokenParams = parameters);
        var server = CreateCodeFlowServerWithCustomHandler<NoSecretRegistrationHandler>(options =>
        {
            options.ClientSecret = "default-secret";
        }, backchannel: backchannel);

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.NotNull(capturedTokenParams);
        // ClientSecret was null in the registration, so no client_secret should be sent
        // (the OpenIdConnectMessage won't include a null/empty parameter)
        Assert.False(capturedTokenParams!.ContainsKey("client_secret") && !string.IsNullOrEmpty(capturedTokenParams["client_secret"]));
    }

    [Fact]
    public async Task ResolveClientRegistration_DefaultReturnsOptionsClientSecret()
    {
        IDictionary<string, string>? capturedTokenParams = null;

        var backchannel = new TokenCapturingBackchannel(parameters => capturedTokenParams = parameters);
        var server = CreateCodeFlowServerWithEvents(options =>
        {
            options.ClientSecret = "my-secret";
        }, backchannel);

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.NotNull(capturedTokenParams);
        Assert.Equal("my-secret", capturedTokenParams!["client_secret"]);
    }

    [Fact]
    public async Task ResolveClientRegistration_OverrideClientSecretUsedInPAR()
    {
        var mockBackchannel = new ParCapturingBackchannel();

        var server = CreateServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.ClientId = "default-client-id";
            options.ClientSecret = "default-secret";
            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Require;
            options.Configuration = new OpenIdConnectConfiguration
            {
                AuthorizationEndpoint = "https://testauthority/authorize",
                PushedAuthorizationRequestEndpoint = "https://testauthority/par"
            };
            options.Events.OnRedirectToIdentityProvider = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };
        }, mockBackchannel);

        await server.SendAsync(ChallengeEndpoint);

        Assert.Equal("dynamic-secret", mockBackchannel.PushedParameters["client_secret"]);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ResolveClientRegistrationAsync — TokenValidationParameters tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveClientRegistration_DefaultClonesTokenValidationParametersFromOptions()
    {
        TokenValidationParameters? capturedParams = null;

        var server = CreateCodeFlowServerWithCustomHandler<CapturingRegistrationHandler>(options =>
        {
            options.TokenValidationParameters.ValidAudience = "original-audience";
        }, onTokenValidationParamsCaptured: tvp => capturedParams = tvp);

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.NotNull(capturedParams);
        Assert.Equal("original-audience", capturedParams!.ValidAudience);
    }

    [Fact]
    public async Task ResolveClientRegistration_OverrideCustomizesTokenValidationParameters()
    {
        TokenValidationParameters? capturedParams = null;

        var server = CreateCodeFlowServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.TokenValidationParameters.ValidAudience = "original-audience";
        }, onTokenValidationParamsCaptured: tvp => capturedParams = tvp);

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.NotNull(capturedParams);
        Assert.Equal("dynamic-client-id", capturedParams!.ValidAudience);
    }

    [Fact]
    public async Task ResolveClientRegistration_OverrideDoesNotMutateOriginalOptions()
    {
        TokenValidationParameters? originalParams = null;

        var server = CreateCodeFlowServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.TokenValidationParameters.ValidAudience = "original-audience";
            originalParams = options.TokenValidationParameters;
        });

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.Equal("original-audience", originalParams!.ValidAudience);
    }

    // ─────────────────────────────────────────────────────────────────────
    // HandleClientAuthentication tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleClientAuthentication_RemovesRegistrationClientSecret()
    {
        IDictionary<string, string>? capturedTokenParams = null;

        var backchannel = new TokenCapturingBackchannel(parameters => capturedTokenParams = parameters);
        var server = CreateCodeFlowServerWithEvents(options =>
        {
            options.ClientSecret = "my-secret";
            options.Events.OnAuthorizationCodeReceived = ctx =>
            {
                ctx.HandleClientAuthentication();
                ctx.TokenEndpointRequest!.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                ctx.TokenEndpointRequest.ClientAssertion = "my_jwt_assertion";
                return Task.CompletedTask;
            };
        }, backchannel);

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.NotNull(capturedTokenParams);
        Assert.DoesNotContain("client_secret", capturedTokenParams!.Keys);
        Assert.Equal("urn:ietf:params:oauth:client-assertion-type:jwt-bearer", capturedTokenParams["client_assertion_type"]);
        Assert.Equal("my_jwt_assertion", capturedTokenParams["client_assertion"]);
    }

    [Fact]
    public void HandleClientAuthentication_ThrowsWhenCodeRedemptionAlreadyHandled()
    {
        var context = new AuthorizationCodeReceivedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("test", "test", typeof(OpenIdConnectHandler)),
            new OpenIdConnectOptions(),
            new AuthenticationProperties());

        context.HandleCodeRedemption();

        Assert.Throws<InvalidOperationException>(() => context.HandleClientAuthentication());
    }

    [Fact]
    public void HandleClientAuthentication_SetsFlag()
    {
        var context = new AuthorizationCodeReceivedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("test", "test", typeof(OpenIdConnectHandler)),
            new OpenIdConnectOptions(),
            new AuthenticationProperties());

        Assert.False(context.HandledClientAuthentication);

        context.HandleClientAuthentication();

        Assert.True(context.HandledClientAuthentication);
    }

    [Fact]
    public async Task HandleClientAuthentication_WithDynamicRegistration_BothApplied()
    {
        IDictionary<string, string>? capturedTokenParams = null;

        var backchannel = new TokenCapturingBackchannel(parameters => capturedTokenParams = parameters);
        var server = CreateCodeFlowServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.ClientSecret = "default-secret";
            options.Events.OnAuthorizationCodeReceived = ctx =>
            {
                ctx.HandleClientAuthentication();
                ctx.TokenEndpointRequest!.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                ctx.TokenEndpointRequest.ClientAssertion = "federated_assertion";
                return Task.CompletedTask;
            };
        }, backchannel: backchannel);

        await PostSignIn(server, "state=protected_state&code=my_code");

        Assert.NotNull(capturedTokenParams);
        Assert.Equal("dynamic-client-id", capturedTokenParams!["client_id"]);
        Assert.DoesNotContain("client_secret", capturedTokenParams.Keys);
        Assert.Equal("federated_assertion", capturedTokenParams["client_assertion"]);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Combined extension points tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AllExtensionPoints_WorkTogether()
    {
        string? challengeClientId = null;
        string? validationClientId = null;
        string? tokenRequestClientId = null;
        TokenValidationParameters? capturedParams = null;
        bool clientAuthHandled = false;

        var challengeServer = CreateServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.ClientId = "default-client-id";
            options.Events.OnRedirectToIdentityProvider = ctx =>
            {
                challengeClientId = ctx.ProtocolMessage.ClientId;
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };
        });

        await challengeServer.SendAsync(ChallengeEndpoint);
        Assert.Equal("dynamic-client-id", challengeClientId);

        var codeServer = CreateCodeFlowServerWithCustomHandler<DynamicRegistrationHandler>(options =>
        {
            options.ClientSecret = "default-secret";
            options.ProtocolValidator = new CapturingProtocolValidator(id => validationClientId = id);
            options.Events.OnAuthorizationCodeReceived = ctx =>
            {
                tokenRequestClientId = ctx.TokenEndpointRequest?.ClientId;
                ctx.HandleClientAuthentication();
                ctx.TokenEndpointRequest!.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                ctx.TokenEndpointRequest.ClientAssertion = "federated_jwt";
                clientAuthHandled = true;
                return Task.CompletedTask;
            };
        }, onTokenValidationParamsCaptured: tvp => capturedParams = tvp);

        await PostSignIn(codeServer, "state=protected_state&code=my_code");

        Assert.Equal("dynamic-client-id", tokenRequestClientId);
        Assert.Equal("dynamic-client-id", validationClientId);
        Assert.NotNull(capturedParams);
        Assert.Equal("dynamic-client-id", capturedParams!.ValidAudience);
        Assert.True(clientAuthHandled);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Custom handler implementations for testing
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simulates a multi-tenant scenario where ClientId, ClientSecret, and TokenValidationParameters
    /// are all resolved dynamically per request.
    /// </summary>
    private class DynamicRegistrationHandler : OpenIdConnectHandler
    {
        public static Action<TokenValidationParameters>? OnParametersCreated;

        public DynamicRegistrationHandler(IOptionsMonitor<OpenIdConnectOptions> options, ILoggerFactory logger, HtmlEncoder htmlEncoder, UrlEncoder encoder)
            : base(options, logger, htmlEncoder, encoder)
        {
        }

        protected override ValueTask<OpenIdConnectClientRegistration> ResolveClientRegistrationAsync(AuthenticationProperties properties)
        {
            var tvp = Options.TokenValidationParameters.Clone();
            tvp.ValidAudience = "dynamic-client-id";
            OnParametersCreated?.Invoke(tvp);

            return ValueTask.FromResult(new OpenIdConnectClientRegistration
            {
                ClientId = "dynamic-client-id",
                ClientSecret = "dynamic-secret",
                TokenValidationParameters = tvp,
            });
        }
    }

    /// <summary>
    /// Handler that returns a registration with no client secret (e.g., public client or assertion-based auth).
    /// </summary>
    private class NoSecretRegistrationHandler : OpenIdConnectHandler
    {
        public NoSecretRegistrationHandler(IOptionsMonitor<OpenIdConnectOptions> options, ILoggerFactory logger, HtmlEncoder htmlEncoder, UrlEncoder encoder)
            : base(options, logger, htmlEncoder, encoder)
        {
        }

        protected override ValueTask<OpenIdConnectClientRegistration> ResolveClientRegistrationAsync(AuthenticationProperties properties)
        {
            return ValueTask.FromResult(new OpenIdConnectClientRegistration
            {
                ClientId = "no-secret-client",
                ClientSecret = null,
                TokenValidationParameters = Options.TokenValidationParameters.Clone(),
            });
        }
    }

    /// <summary>
    /// Handler that captures the token validation parameters without modifying them,
    /// to verify the default clone behavior.
    /// </summary>
    private class CapturingRegistrationHandler : OpenIdConnectHandler
    {
        public static Action<TokenValidationParameters>? OnParametersCreated;

        public CapturingRegistrationHandler(IOptionsMonitor<OpenIdConnectOptions> options, ILoggerFactory logger, HtmlEncoder htmlEncoder, UrlEncoder encoder)
            : base(options, logger, htmlEncoder, encoder)
        {
        }

        protected override ValueTask<OpenIdConnectClientRegistration> ResolveClientRegistrationAsync(AuthenticationProperties properties)
        {
            var tvp = Options.TokenValidationParameters.Clone();
            OnParametersCreated?.Invoke(tvp);

            return ValueTask.FromResult(new OpenIdConnectClientRegistration
            {
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                TokenValidationParameters = tvp,
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test infrastructure
    // ─────────────────────────────────────────────────────────────────────

    private static TestServer CreateServerWithEvents(Action<OpenIdConnectOptions> configureOptions)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(auth =>
                    {
                        auth.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        auth.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                    .AddCookie()
                    .AddOpenIdConnect(o =>
                    {
                        o.ClientId = "TestClientId";
                        o.Configuration = TestServerBuilder.CreateDefaultOpenIdConnectConfiguration();
                        configureOptions(o);
                    });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Path == new PathString(TestServerBuilder.Challenge))
                        {
                            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
                        }
                        else
                        {
                            await next(context);
                        }
                    });
                }))
            .Build();

        host.Start();
        return host.GetTestServer();
    }

    private static TestServer CreateServerWithCustomHandler<THandler>(
        Action<OpenIdConnectOptions> configureOptions,
        HttpMessageHandler? backchannel = null) where THandler : OpenIdConnectHandler
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(auth =>
                    {
                        auth.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        auth.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                    .AddCookie()
                    .AddOpenIdConnect(o =>
                    {
                        o.ClientId = "TestClientId";
                        o.Configuration = TestServerBuilder.CreateDefaultOpenIdConnectConfiguration();
                        if (backchannel is not null)
                        {
                            o.BackchannelHttpHandler = backchannel;
                        }
                        configureOptions(o);
                    });

                    services.AddTransient<THandler>();
                    services.Configure<AuthenticationOptions>(auth =>
                    {
                        var scheme = auth.SchemeMap[OpenIdConnectDefaults.AuthenticationScheme];
                        scheme.HandlerType = typeof(THandler);
                    });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Path == new PathString(TestServerBuilder.Challenge))
                        {
                            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
                        }
                        else
                        {
                            await next(context);
                        }
                    });
                }))
            .Build();

        host.Start();
        return host.GetTestServer();
    }

    private static TestServer CreateCodeFlowServerWithEvents(
        Action<OpenIdConnectOptions> configureOptions,
        HttpMessageHandler? backchannel = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(auth =>
                    {
                        auth.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        auth.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                    .AddCookie()
                    .AddOpenIdConnect(o =>
                    {
                        o.ClientId = "ClientId";
                        o.GetClaimsFromUserInfoEndpoint = false;
                        o.Configuration = new OpenIdConnectConfiguration()
                        {
                            TokenEndpoint = "http://testhost/tokens",
                            UserInfoEndpoint = "http://testhost/user",
                            EndSessionEndpoint = "http://testhost/end"
                        };
                        o.StateDataFormat = new TestStateDataFormat();
                        o.UseSecurityTokenValidator = false;
                        o.TokenHandler = new TestTokenHandler();
                        o.ProtocolValidator = new TestProtocolValidator();
                        o.BackchannelHttpHandler = backchannel ?? new TestBackchannel();
                        configureOptions(o);
                    });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        return Task.CompletedTask;
                    });
                }))
            .Build();

        host.Start();
        return host.GetTestServer();
    }

    private static TestServer CreateCodeFlowServerWithCustomHandler<THandler>(
        Action<OpenIdConnectOptions> configureOptions,
        Action<TokenValidationParameters>? onTokenValidationParamsCaptured = null,
        HttpMessageHandler? backchannel = null) where THandler : OpenIdConnectHandler
    {
        if (typeof(THandler) == typeof(DynamicRegistrationHandler))
        {
            DynamicRegistrationHandler.OnParametersCreated = onTokenValidationParamsCaptured;
        }
        else if (typeof(THandler) == typeof(CapturingRegistrationHandler))
        {
            CapturingRegistrationHandler.OnParametersCreated = onTokenValidationParamsCaptured;
        }

        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(auth =>
                    {
                        auth.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        auth.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                    .AddCookie()
                    .AddOpenIdConnect(o =>
                    {
                        o.ClientId = "ClientId";
                        o.GetClaimsFromUserInfoEndpoint = false;
                        o.Configuration = new OpenIdConnectConfiguration()
                        {
                            TokenEndpoint = "http://testhost/tokens",
                            UserInfoEndpoint = "http://testhost/user",
                            EndSessionEndpoint = "http://testhost/end"
                        };
                        o.StateDataFormat = new TestStateDataFormat();
                        o.UseSecurityTokenValidator = false;
                        o.TokenHandler = new TestTokenHandler();
                        o.ProtocolValidator = new TestProtocolValidator();
                        o.BackchannelHttpHandler = backchannel ?? new TestBackchannel();
                        configureOptions(o);
                    });

                    services.AddTransient<THandler>();
                    services.Configure<AuthenticationOptions>(auth =>
                    {
                        var scheme = auth.SchemeMap[OpenIdConnectDefaults.AuthenticationScheme];
                        scheme.HandlerType = typeof(THandler);
                    });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        return Task.CompletedTask;
                    });
                }))
            .Build();

        host.Start();
        return host.GetTestServer();
    }

    private static Task<HttpResponseMessage> PostSignIn(TestServer server, string form)
    {
        var client = server.CreateClient();
        var cookie = ".AspNetCore.Correlation.correlationId=N";
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client.PostAsync("signin-oidc",
            new StringContent(form, Encoding.ASCII, "application/x-www-form-urlencoded"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test doubles
    // ─────────────────────────────────────────────────────────────────────

    private class TestStateDataFormat : ISecureDataFormat<AuthenticationProperties>
    {
        public string Protect(AuthenticationProperties data) => "protected_state";

        public string Protect(AuthenticationProperties data, string purpose) =>
            throw new NotImplementedException();

        public AuthenticationProperties Unprotect(string protectedText)
        {
            Assert.Equal("protected_state", protectedText);
            var properties = new AuthenticationProperties(new Dictionary<string, string>()
            {
                { ".xsrf", "correlationId" },
                { OpenIdConnectDefaults.RedirectUriForCodePropertiesKey, "redirect_uri" },
                { "testkey", "testvalue" }
            });
            properties.RedirectUri = "http://testhost/redirect";
            return properties;
        }

        public AuthenticationProperties Unprotect(string protectedText, string purpose) =>
            throw new NotImplementedException();
    }

    private class TestTokenHandler : TokenHandler
    {
        public override Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
        {
            Assert.Equal("my_id_token", token);
            var jwt = new JwtSecurityToken();
            return Task.FromResult(new TokenValidationResult()
            {
                SecurityToken = new JsonWebToken(jwt.EncodedHeader + "." + jwt.EncodedPayload + "."),
                ClaimsIdentity = new ClaimsIdentity("customAuthType"),
                IsValid = true
            });
        }

        public override SecurityToken ReadToken(string token)
        {
            Assert.Equal("my_id_token", token);
            return new JsonWebToken(token);
        }
    }

    private class TestProtocolValidator : OpenIdConnectProtocolValidator
    {
        public override void ValidateAuthenticationResponse(OpenIdConnectProtocolValidationContext validationContext) { }
        public override void ValidateTokenResponse(OpenIdConnectProtocolValidationContext validationContext) { }
        public override void ValidateUserInfoResponse(OpenIdConnectProtocolValidationContext validationContext) { }
    }

    private class CapturingProtocolValidator : OpenIdConnectProtocolValidator
    {
        private readonly Action<string> _captureClientId;

        public CapturingProtocolValidator(Action<string> captureClientId)
        {
            _captureClientId = captureClientId;
        }

        public override void ValidateAuthenticationResponse(OpenIdConnectProtocolValidationContext validationContext)
        {
            _captureClientId(validationContext.ClientId);
        }

        public override void ValidateTokenResponse(OpenIdConnectProtocolValidationContext validationContext)
        {
            _captureClientId(validationContext.ClientId);
        }

        public override void ValidateUserInfoResponse(OpenIdConnectProtocolValidationContext validationContext) { }
    }

    private class TestBackchannel : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (string.Equals("/tokens", request.RequestUri?.AbsolutePath, StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage()
                {
                    Content = new StringContent(
                        "{ \"id_token\": \"my_id_token\", \"access_token\": \"my_access_token\" }",
                        Encoding.ASCII,
                        "application/json")
                });
            }
            if (string.Equals("/user", request.RequestUri?.AbsolutePath, StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage()
                {
                    Content = new StringContent("{ }", Encoding.ASCII, "application/json")
                });
            }

            throw new NotImplementedException(request.RequestUri?.ToString());
        }
    }

    private class TokenCapturingBackchannel : HttpMessageHandler
    {
        private readonly Action<IDictionary<string, string>> _captureParams;

        public TokenCapturingBackchannel(Action<IDictionary<string, string>> captureParams)
        {
            _captureParams = captureParams;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (string.Equals("/tokens", request.RequestUri?.AbsolutePath, StringComparison.Ordinal))
            {
                var content = await request.Content!.ReadAsStringAsync();
                var query = HttpUtility.ParseQueryString(content);
                var parameters = new Dictionary<string, string>();
                foreach (string key in query)
                {
                    if (key is not null)
                    {
                        parameters[key] = query[key]!;
                    }
                }
                _captureParams(parameters);

                return new HttpResponseMessage()
                {
                    Content = new StringContent(
                        "{ \"id_token\": \"my_id_token\", \"access_token\": \"my_access_token\" }",
                        Encoding.ASCII,
                        "application/json")
                };
            }
            if (string.Equals("/user", request.RequestUri?.AbsolutePath, StringComparison.Ordinal))
            {
                return new HttpResponseMessage()
                {
                    Content = new StringContent("{ }", Encoding.ASCII, "application/json")
                };
            }

            throw new NotImplementedException(request.RequestUri?.ToString());
        }
    }

    private class ParCapturingBackchannel : HttpMessageHandler
    {
        public IDictionary<string, string> PushedParameters { get; set; } = new Dictionary<string, string>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (string.Equals("/par", request.RequestUri?.AbsolutePath, StringComparison.Ordinal))
            {
                var content = await request.Content!.ReadAsStringAsync();
                var query = HttpUtility.ParseQueryString(content);
                foreach (string key in query)
                {
                    if (key is not null)
                    {
                        PushedParameters[key] = query[key]!;
                    }
                }
                return new HttpResponseMessage()
                {
                    Content = new StringContent(
                        "{ \"request_uri\": \"my_reference_value\", \"expires_in\": 60 }",
                        Encoding.ASCII,
                        "application/json")
                };
            }

            throw new NotImplementedException(request.RequestUri?.ToString());
        }
    }
}
