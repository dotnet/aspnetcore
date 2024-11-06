// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

public class OpenIdConnectChallengeTests
{
    private static readonly string ChallengeEndpoint = TestServerBuilder.TestHost + TestServerBuilder.Challenge;

    [Fact]
    public async Task ChallengeRedirectIsIssuedCorrectly()
    {
        var settings = new TestSettings(
            opt =>
            {
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                opt.ClientId = "Test Id";
            });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        settings.ValidateChallengeRedirect(
            res.Headers.Location,
            OpenIdConnectParameterNames.ClientId,
            OpenIdConnectParameterNames.ResponseType,
            OpenIdConnectParameterNames.ResponseMode,
            OpenIdConnectParameterNames.Scope,
            OpenIdConnectParameterNames.RedirectUri,
            OpenIdConnectParameterNames.SkuTelemetry,
            OpenIdConnectParameterNames.VersionTelemetry);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChallengeIncludesPkceIfRequested(bool include)
    {
        var settings = new TestSettings(
            opt =>
            {
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                opt.ResponseType = OpenIdConnectResponseType.Code;
                opt.ClientId = "Test Id";
                opt.UsePkce = include;
            });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        if (include)
        {
            Assert.Contains("code_challenge=", res.Headers.Location.Query);
            Assert.Contains("code_challenge_method=S256", res.Headers.Location.Query);
        }
        else
        {
            Assert.DoesNotContain("code_challenge=", res.Headers.Location.Query);
            Assert.DoesNotContain("code_challenge_method=", res.Headers.Location.Query);
        }
    }

    [Theory]
    [InlineData(OpenIdConnectResponseType.Token)]
    [InlineData(OpenIdConnectResponseType.IdToken)]
    [InlineData(OpenIdConnectResponseType.CodeIdToken)]
    public async Task ChallengeDoesNotIncludePkceForOtherResponseTypes(string responseType)
    {
        var settings = new TestSettings(
            opt =>
            {
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                opt.ResponseType = responseType;
                opt.ClientId = "Test Id";
                opt.UsePkce = true;
            });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        Assert.DoesNotContain("code_challenge=", res.Headers.Location.Query);
        Assert.DoesNotContain("code_challenge_method=", res.Headers.Location.Query);
    }

    [Fact]
    public async Task AuthorizationRequestDoesNotIncludeTelemetryParametersWhenDisabled()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.DisableTelemetry = true;
        });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.DoesNotContain(OpenIdConnectParameterNames.SkuTelemetry, res.Headers.Location.Query);
        Assert.DoesNotContain(OpenIdConnectParameterNames.VersionTelemetry, res.Headers.Location.Query);
    }

    /*
    Example of a form post
    <body>
        <form name=\ "form\" method=\ "post\" action=\ "https://login.microsoftonline.com/common/oauth2/authorize\">
            <input type=\ "hidden\" name=\ "client_id\" value=\ "51e38103-238f-410f-a5d5-61991b203e50\" />
            <input type=\ "hidden\" name=\ "redirect_uri\" value=\ "https://example.com/signin-oidc\" />
            <input type=\ "hidden\" name=\ "response_type\" value=\ "id_token\" />
            <input type=\ "hidden\" name=\ "scope\" value=\ "openid profile\" />
            <input type=\ "hidden\" name=\ "response_mode\" value=\ "form_post\" />
            <input type=\ "hidden\" name=\ "nonce\" value=\ "636072461997914230.NTAwOGE1MjQtM2VhYS00ZDU0LWFkYzYtNmZiYWE2MDRkODg3OTlkMDFmOWUtOTMzNC00ZmI2LTg1Y2YtOWM4OTlhNjY0Yjli\" />
            <input type=\ "hidden\" name=\ "state\" value=\
                "CfDJ8Jh1NKaF0T5AnK4qsqzzIs89srKe4iEaBWd29MNph4Ki887QKgkD24wjhZ0ciH-ar6A_jUmRI2O5haXN2-YXbC0ZRuRAvNsx5LqbPTdh4MJBIwXWkG_rM0T0tI3h5Y2pDttWSaku6a_nzFLUYBrKfsE7sDLVoTDrzzOcHrRQhdztqOOeNUuu2wQXaKwlOtNI21ShtN9EVxvSGFOxUUOwVih4nFdF40fBcbsuPpcpCPkLARQaFRJSYsNKiP7pcFMnRwzZhnISHlyGKkzwJ1DIx7nsmdiQFBGljimw5GnYAs-5ru9L3w8NnPjkl96OyQ8MJOcayMDmOY26avs2sYP_Zw0\" />
            <noscript>Click here to finish the process: <input type=\"submit\" /></noscript>
        </form>
        <script>
            document.form.submit();
        </script>
    </body>
    */
    [Fact]
    public async Task ChallengeFormPostIssuedCorrectly()
    {
        var settings = new TestSettings(
            opt =>
            {
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.AuthenticationMethod = OpenIdConnectRedirectBehavior.FormPost;
                opt.ClientId = "Test Id";
            });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("text/html", transaction.Response.Content.Headers.ContentType.MediaType);

        var body = await res.Content.ReadAsStringAsync();
        settings.ValidateChallengeFormPost(
            body,
            OpenIdConnectParameterNames.ClientId,
            OpenIdConnectParameterNames.ResponseType,
            OpenIdConnectParameterNames.ResponseMode,
            OpenIdConnectParameterNames.Scope,
            OpenIdConnectParameterNames.RedirectUri);
    }

    [Theory]
    [InlineData("sample_user_state")]
    [InlineData(null)]
    public async Task ChallengeCanSetUserStateThroughProperties(string userState)
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("OIDCTest"));
        var settings = new TestSettings(o =>
        {
            o.ClientId = "Test Id";
            o.Authority = TestServerBuilder.DefaultAuthority;
            o.StateDataFormat = stateFormat;
        });

        var properties = new AuthenticationProperties();
        properties.Items.Add(OpenIdConnectDefaults.UserstatePropertiesKey, userState);

        var server = settings.CreateTestServer(properties);
        var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        var values = settings.ValidateChallengeRedirect(res.Headers.Location);
        var actualState = values[OpenIdConnectParameterNames.State];
        var actualProperties = stateFormat.Unprotect(actualState);

        Assert.Equal(userState ?? string.Empty, actualProperties.Items[OpenIdConnectDefaults.UserstatePropertiesKey]);
    }

    [Theory]
    [InlineData("sample_user_state")]
    [InlineData(null)]
    public async Task OnRedirectToIdentityProviderEventCanSetState(string userState)
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("OIDCTest"));
        var settings = new TestSettings(opt =>
        {
            opt.StateDataFormat = stateFormat;
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.Events = new OpenIdConnectEvents()
            {
                OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.State = userState;
                    return Task.FromResult(0);
                }
            };
        });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        var values = settings.ValidateChallengeRedirect(res.Headers.Location);
        var actualState = values[OpenIdConnectParameterNames.State];
        var actualProperties = stateFormat.Unprotect(actualState);

        if (userState != null)
        {
            Assert.Equal(userState, actualProperties.Items[OpenIdConnectDefaults.UserstatePropertiesKey]);
        }
        else
        {
            Assert.False(actualProperties.Items.ContainsKey(OpenIdConnectDefaults.UserstatePropertiesKey));
        }
    }

    [Fact]
    public async Task OnRedirectToIdentityProviderEventIsHit()
    {
        var eventIsHit = false;
        var settings = new TestSettings(
            opts =>
            {
                opts.ClientId = "Test Id";
                opts.Authority = TestServerBuilder.DefaultAuthority;
                opts.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        eventIsHit = true;
                        return Task.FromResult(0);
                    }
                };
            }
        );

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        Assert.True(eventIsHit);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        settings.ValidateChallengeRedirect(
            res.Headers.Location,
            OpenIdConnectParameterNames.ClientId,
            OpenIdConnectParameterNames.ResponseType,
            OpenIdConnectParameterNames.ResponseMode,
            OpenIdConnectParameterNames.Scope,
            OpenIdConnectParameterNames.RedirectUri);
    }

    [Fact]
    public async Task OnRedirectToIdentityProviderEventCanReplaceValues()
    {
        var newClientId = Guid.NewGuid().ToString();

        var settings = new TestSettings(
            opts =>
            {
                opts.ClientId = "Test Id";
                opts.Authority = TestServerBuilder.DefaultAuthority;
                opts.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.ClientId = newClientId;
                        return Task.FromResult(0);
                    }
                };
            }
        );

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        settings.ValidateChallengeRedirect(
            res.Headers.Location,
            OpenIdConnectParameterNames.ResponseType,
            OpenIdConnectParameterNames.ResponseMode,
            OpenIdConnectParameterNames.Scope,
            OpenIdConnectParameterNames.RedirectUri);

        var actual = res.Headers.Location.Query.Trim('?').Split('&').Single(seg => seg.StartsWith($"{OpenIdConnectParameterNames.ClientId}=", StringComparison.Ordinal));
        Assert.Equal($"{OpenIdConnectParameterNames.ClientId}={newClientId}", actual);
    }

    [Fact]
    public async Task OnRedirectToIdentityProviderEventCanReplaceMessage()
    {
        var newMessage = new MockOpenIdConnectMessage
        {
            IssuerAddress = "http://example.com/",
            TestAuthorizeEndpoint = $"http://example.com/{Guid.NewGuid()}/oauth2/signin"
        };

        var settings = new TestSettings(
            opts =>
            {
                opts.ClientId = "Test Id";
                opts.Authority = TestServerBuilder.DefaultAuthority;
                opts.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage = newMessage;

                        return Task.FromResult(0);
                    }
                };
            }
        );

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        // The CreateAuthenticationRequestUrl method is overridden MockOpenIdConnectMessage where
        // query string is not generated and the authorization endpoint is replaced.
        Assert.Equal(newMessage.TestAuthorizeEndpoint, res.Headers.Location.AbsoluteUri);
    }

    [Fact]
    public async Task OnRedirectToIdentityProviderEventHandlesResponse()
    {
        var settings = new TestSettings(
            opts =>
            {
                opts.ClientId = "Test Id";
                opts.Authority = TestServerBuilder.DefaultAuthority;
                opts.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.Response.StatusCode = 410;
                        context.Response.Headers.Add("tea", "Oolong");
                        context.HandleResponse();

                        return Task.FromResult(0);
                    }
                };
            }
        );

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.Gone, res.StatusCode);
        Assert.Equal("Oolong", res.Headers.GetValues("tea").Single());
        Assert.Null(res.Headers.Location);
    }

    // This test can be further refined. When one auth handler skips, the authentication responsibility
    // will be flowed to the next one. A dummy auth handler can be added to ensure the correct logic.
    [Fact]
    public async Task OnRedirectToIdentityProviderEventHandleResponse()
    {
        var settings = new TestSettings(
            opts =>
            {
                opts.ClientId = "Test Id";
                opts.Authority = TestServerBuilder.DefaultAuthority;
                opts.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.HandleResponse();
                        return Task.FromResult(0);
                    }
                };
            }
        );

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Null(res.Headers.Location);
    }

    [Theory]
    [InlineData(OpenIdConnectRedirectBehavior.RedirectGet)]
    [InlineData(OpenIdConnectRedirectBehavior.FormPost)]
    public async Task ChallengeSetsNonceAndStateCookies(OpenIdConnectRedirectBehavior method)
    {
        var settings = new TestSettings(o =>
        {
            o.AuthenticationMethod = method;
            o.ClientId = "Test Id";
            o.Authority = TestServerBuilder.DefaultAuthority;
        });
        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        Assert.Contains("samesite=none", transaction.SetCookie.First());
        var challengeCookies = SetCookieHeaderValue.ParseList(transaction.SetCookie);
        var nonceCookie = challengeCookies.Where(cookie => cookie.Name.StartsWith(OpenIdConnectDefaults.CookieNoncePrefix, StringComparison.Ordinal)).Single();
        Assert.True(nonceCookie.Expires.HasValue);
        Assert.True(nonceCookie.Expires > DateTime.UtcNow);
        Assert.True(nonceCookie.HttpOnly);
        Assert.Equal("/signin-oidc", nonceCookie.Path.AsSpan());
        Assert.Equal("N", nonceCookie.Value.AsSpan());
        Assert.Equal(Net.Http.Headers.SameSiteMode.None, nonceCookie.SameSite);

        var correlationCookie = challengeCookies.Where(cookie => cookie.Name.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal)).Single();
        Assert.True(correlationCookie.Expires.HasValue);
        Assert.True(nonceCookie.Expires > DateTime.UtcNow);
        Assert.True(correlationCookie.HttpOnly);
        Assert.Equal("/signin-oidc", correlationCookie.Path.AsSpan());
        Assert.False(StringSegment.IsNullOrEmpty(correlationCookie.Value));
        Assert.Equal(Net.Http.Headers.SameSiteMode.None, correlationCookie.SameSite);

        Assert.Equal(2, challengeCookies.Count);
    }

    [Fact]
    public async Task Challenge_WithEmptyConfig_Fails()
    {
        var settings = new TestSettings(
            opt =>
            {
                opt.ClientId = "Test Id";
                opt.Configuration = new OpenIdConnectConfiguration();
            });

        var server = settings.CreateTestServer();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync(ChallengeEndpoint));
        Assert.Equal("Cannot redirect to the authorization endpoint, the configuration may be missing or invalid.", exception.Message);
    }

    [Fact]
    public async Task Challenge_WithDefaultMaxAge_HasExpectedMaxAgeParam()
    {
        var settings = new TestSettings(
            opt =>
            {
                opt.ClientId = "Test Id";
                opt.Authority = TestServerBuilder.DefaultAuthority;
            });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(
            res.Headers.Location,
            OpenIdConnectParameterNames.MaxAge);
    }

    [Fact]
    public async Task Challenge_WithSpecificMaxAge_HasExpectedMaxAgeParam()
    {
        var settings = new TestSettings(
            opt =>
            {
                opt.ClientId = "Test Id";
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.MaxAge = TimeSpan.FromMinutes(20);
            });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(
            res.Headers.Location,
            OpenIdConnectParameterNames.MaxAge);
    }

    [Fact]
    public async Task Challenge_HasExpectedPromptParam()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.Prompt = "consent";
        });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(res.Headers.Location, OpenIdConnectParameterNames.Prompt);
        Assert.Contains("prompt=consent", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_HasOverwrittenPromptParam()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.Prompt = "consent";
        });
        var properties = new OpenIdConnectChallengeProperties()
        {
            Prompt = "login",
        };

        var server = settings.CreateTestServer(properties);
        var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(res.Headers.Location);
        Assert.Contains("prompt=login", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_HasOverwrittenPromptParamFromBaseAuthenticationProperties()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.Prompt = "consent";
        });
        var properties = new AuthenticationProperties();
        properties.SetParameter(OpenIdConnectChallengeProperties.PromptKey, "login");

        var server = settings.CreateTestServer(properties);
        var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(res.Headers.Location);
        Assert.Contains("prompt=login", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_HasOverwrittenScopeParam()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.Scope.Clear();
            opt.Scope.Add("foo");
            opt.Scope.Add("bar");
        });
        var properties = new OpenIdConnectChallengeProperties();
        properties.SetScope("baz", "qux");

        var server = settings.CreateTestServer(properties);
        var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(res.Headers.Location);
        Assert.Contains("scope=baz%20qux", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_HasOverwrittenScopeParamFromBaseAuthenticationProperties()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.Scope.Clear();
            opt.Scope.Add("foo");
            opt.Scope.Add("bar");
        });
        var properties = new AuthenticationProperties();
        properties.SetParameter(OpenIdConnectChallengeProperties.ScopeKey, new string[] { "baz", "qux" });

        var server = settings.CreateTestServer(properties);
        var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(res.Headers.Location);
        Assert.Contains("scope=baz%20qux", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_HasOverwrittenMaxAgeParam()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.MaxAge = TimeSpan.FromSeconds(500);
        });
        var properties = new OpenIdConnectChallengeProperties()
        {
            MaxAge = TimeSpan.FromSeconds(1234),
        };

        var server = settings.CreateTestServer(properties);
        var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(res.Headers.Location);
        Assert.Contains("max_age=1234", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_HasOverwrittenMaxAgeParaFromBaseAuthenticationPropertiesm()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.MaxAge = TimeSpan.FromSeconds(500);
        });
        var properties = new AuthenticationProperties();
        properties.SetParameter(OpenIdConnectChallengeProperties.MaxAgeKey, TimeSpan.FromSeconds(1234));

        var server = settings.CreateTestServer(properties);
        var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(res.Headers.Location);
        Assert.Contains("max_age=1234", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_WithAdditionalAuthorizationParameters()
    {
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.Authority = TestServerBuilder.DefaultAuthority;
            opt.AdditionalAuthorizationParameters.Add("prompt", "login");
            opt.AdditionalAuthorizationParameters.Add("audience", "https://api.example.com");
        });

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        settings.ValidateChallengeRedirect(res.Headers.Location);
        Assert.Contains("prompt=login&audience=https%3A%2F%2Fapi.example.com", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_WithPushedAuthorization()
    {
        var mockBackchannel = new ParMockBackchannel();
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.ClientSecret = "secret";

            opt.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable;
            // Instead of using discovery, this test hard codes the configuration that disco would retrieve.
            // This makes it easier to manipulate the discovery results
            opt.Configuration = new OpenIdConnectConfiguration();
            opt.Configuration.AuthorizationEndpoint = "https://testauthority/authorize";
            opt.Configuration.PushedAuthorizationRequestEndpoint = "https://testauthority/par";
        }, mockBackchannel);

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        // We should be redirected with a request_uri and client_id, as per spec
        Assert.Contains("request_uri=my_reference_value", res.Headers.Location.Query);
        Assert.Contains("client_id=Test%20Id", res.Headers.Location.Query);

        // The request_uri takes the place of all the other usual parameters
        Assert.DoesNotContain("redirect_uri", res.Headers.Location.Query);
        Assert.DoesNotContain("scope", res.Headers.Location.Query);
        Assert.DoesNotContain("client_secret", res.Headers.Location.Query);

        Assert.Contains("redirect_uri", mockBackchannel.PushedParameters);
        Assert.Contains("scope", mockBackchannel.PushedParameters);
        Assert.Contains("client_secret", mockBackchannel.PushedParameters);
    }

    [Fact]
    public async Task Challenge_WithPushedAuthorization_RequiredByOptions_EndpointMissingInDiscovery()
    {
        var mockBackchannel = new ParMockBackchannel();
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.ClientSecret = "secret";

            opt.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Require;
            // Instead of using discovery, this test hard codes the configuration that disco would retrieve.
            // This makes it easier to manipulate the discovery results
            opt.Configuration = new OpenIdConnectConfiguration();
            opt.Configuration.AuthorizationEndpoint = "https://testauthority/authorize";

            // We are NOT setting the endpoint (as if the disco document didn't contain it)
            //opt.Configuration.PushedAuthorizationRequestEndpoint;
        }, mockBackchannel);

        var server = settings.CreateTestServer();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync(ChallengeEndpoint));
        Assert.Equal("Pushed authorization is required by the OpenIdConnectOptions.PushedAuthorizationBehavior, but no pushed authorization endpoint is available.", exception.Message);
    }

    [Fact]
    public async Task Challenge_WithPushedAuthorization_DisabledByOptions_RequiredInDiscovery()
    {
        var mockBackchannel = new ParMockBackchannel();
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.ClientSecret = "secret";

            opt.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
            // Instead of using discovery, this test hard codes the configuration that disco would retrieve.
            // This makes it easier to manipulate the discovery results
            opt.Configuration = new OpenIdConnectConfiguration();
            opt.Configuration.AuthorizationEndpoint = "https://testauthority/authorize";

            opt.Configuration.RequirePushedAuthorizationRequests = true;
        }, mockBackchannel);

        var server = settings.CreateTestServer();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync(ChallengeEndpoint));
        Assert.Equal("Pushed authorization is required by the OpenId Connect provider, but disabled by the OpenIdConnectOptions.PushedAuthorizationBehavior.", exception.Message);
    }

    [Fact]
    public async Task Challenge_WithPushedAuthorization_Skipped()
    {
        var mockBackchannel = new ParMockBackchannel();
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";

            opt.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable;
            // Instead of using discovery, this test hard codes the configuration that disco would retrieve.
            // This makes it easier to manipulate the discovery results
            opt.Configuration = new OpenIdConnectConfiguration();
            opt.Configuration.AuthorizationEndpoint = "https://testauthority/authorize";
            opt.Configuration.PushedAuthorizationRequestEndpoint = "https://testauthority/par";

            opt.Events.OnPushAuthorization = ctx =>
            {
                ctx.SkipPush();
                return Task.CompletedTask;
            };
        }, mockBackchannel);

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        // We didn't push, so we don't have a request_uri, and the mock didn't capture any values
        Assert.DoesNotContain("request_uri=my_reference_value", res.Headers.Location.Query);
        Assert.Empty(mockBackchannel.PushedParameters);

        // All the usual parameters are present
        Assert.Contains("redirect_uri", res.Headers.Location.Query);
        Assert.Contains("scope", res.Headers.Location.Query);
    }

    [Fact]
    public async Task Challenge_WithPushedAuthorization_Handled()
    {
        var mockBackchannel = new ParMockBackchannel();
        var eventProvidedRequestUri = "request_uri_from_event";
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";

            opt.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable;
            // Instead of using discovery, this test hard codes the configuration that disco would retrieve.
            // This makes it easier to manipulate the discovery results
            opt.Configuration = new OpenIdConnectConfiguration();
            opt.Configuration.AuthorizationEndpoint = "https://testauthority/authorize";
            opt.Configuration.PushedAuthorizationRequestEndpoint = "https://testauthority/par";

            opt.Events.OnPushAuthorization = ctx =>
            {
                ctx.HandlePush(eventProvidedRequestUri);
                return Task.CompletedTask;
            };
        }, mockBackchannel);

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        Assert.Contains("request_uri=request_uri_from_event", res.Headers.Location.Query);
        Assert.Empty(mockBackchannel.PushedParameters);
    }

    [Fact]
    public async Task Challenge_WithPushedAuthorization_HandleClientAuthentication()
    {
        var mockBackchannel = new ParMockBackchannel();
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";
            opt.ClientSecret = "secret";

            opt.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable;
            // Instead of using discovery, this test hard codes the configuration that disco would retrieve.
            // This makes it easier to manipulate the discovery results
            opt.Configuration = new OpenIdConnectConfiguration();
            opt.Configuration.AuthorizationEndpoint = "https://testauthority/authorize";
            opt.Configuration.PushedAuthorizationRequestEndpoint = "https://testauthority/par";

            opt.Events.OnPushAuthorization = ctx =>
            {
                ctx.HandleClientAuthentication();
                return Task.CompletedTask;
            };
        }, mockBackchannel);

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        // No secret is set, since the event handled it
        Assert.DoesNotContain("scope", res.Headers.Location.Query);
        Assert.DoesNotContain("client_secret", mockBackchannel.PushedParameters);
    }

    [Fact]
    public async Task Challenge_WithPushedAuthorizationAndAdditionalParameters()
    {
        var mockBackchannel = new ParMockBackchannel();
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";

            opt.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable;
            // Instead of using discovery, this test hard codes the configuration that disco would retrieve.
            // This makes it easier to manipulate the discovery results
            opt.Configuration = new OpenIdConnectConfiguration();
            opt.Configuration.AuthorizationEndpoint = "https://testauthority/authorize";
            opt.Configuration.PushedAuthorizationRequestEndpoint = "https://testauthority/par";

            opt.AdditionalAuthorizationParameters.Add("prompt", "login"); // This should get pushed too, so we don't expect to see it in the authorize redirect.

        }, mockBackchannel);

        var server = settings.CreateTestServer();
        var transaction = await server.SendAsync(ChallengeEndpoint);

        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        // We should be redirected with a request_uri and client_id, as per spec
        Assert.Contains("request_uri=my_reference_value", res.Headers.Location.Query);
        Assert.Contains("client_id=Test%20Id", res.Headers.Location.Query);

        // The request_uri takes the place of all the other usual parameters
        Assert.DoesNotContain("prompt", res.Headers.Location.Query);

        Assert.Equal("login", mockBackchannel.PushedParameters["prompt"]);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public async Task Challenge_WithPushedAuthorization_MultipleContextActionsAreNotAllowed(bool handlePush, bool skipPush, bool handleAuth)
    {
        var mockBackchannel = new ParMockBackchannel();
        var settings = new TestSettings(opt =>
        {
            opt.ClientId = "Test Id";

            // Instead of using discovery, this test hard codes the configuration that disco would retrieve.
            // This makes it easier to manipulate the discovery results
            opt.Configuration = new OpenIdConnectConfiguration();
            opt.Configuration.AuthorizationEndpoint = "https://testauthority/authorize";
            opt.Configuration.PushedAuthorizationRequestEndpoint = "https://testauthority/par";

            opt.Events.OnPushAuthorization = ctx =>
            {
                if (handlePush)
                {
                    ctx.HandlePush("some request uri");
                }
                if (skipPush)
                {
                    ctx.SkipPush();
                }
                if (handleAuth)
                {
                    ctx.HandleClientAuthentication();
                }
                return Task.CompletedTask;
            };

        }, mockBackchannel);

        var server = settings.CreateTestServer();
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync(ChallengeEndpoint));
    }
}

class ParMockBackchannel : HttpMessageHandler
{
    public IDictionary<string, string> PushedParameters { get; set; } = new Dictionary<string, string>();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.Equals("/tokens", request.RequestUri.AbsolutePath, StringComparison.Ordinal))
        {
            return new HttpResponseMessage()
            {
                Content =
                new StringContent("{ \"id_token\": \"my_id_token\", \"access_token\": \"my_access_token\" }", Encoding.ASCII, "application/json")
            };
        }
        if (string.Equals("/user", request.RequestUri.AbsolutePath, StringComparison.Ordinal))
        {
            return new HttpResponseMessage() { Content = new StringContent("{ }", Encoding.ASCII, "application/json") };
        }
        if (string.Equals("/par", request.RequestUri.AbsolutePath, StringComparison.Ordinal))
        {
            var content = await request.Content.ReadAsStringAsync();
            var query = HttpUtility.ParseQueryString(content);
            foreach(string key in query)
            {
                PushedParameters[key] = query[key];
            }
            return new HttpResponseMessage() { Content = new StringContent("{ \"request_uri\": \"my_reference_value\", \"expires_in\": 60}", Encoding.ASCII, "application/json") };
        }

        throw new NotImplementedException(request.RequestUri.ToString());
    }
}
