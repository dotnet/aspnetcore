// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Authentication.Google;

public class GoogleTests : RemoteAuthenticationTests<GoogleOptions>
{
    protected override string DefaultScheme => GoogleDefaults.AuthenticationScheme;
    protected override Type HandlerType => typeof(GoogleHandler);
    protected override bool SupportsSignIn { get => false; }
    protected override bool SupportsSignOut { get => false; }

    protected override void RegisterAuth(AuthenticationBuilder services, Action<GoogleOptions> configure)
    {
        services.AddGoogle(o =>
        {
            ConfigureDefaults(o);
            configure.Invoke(o);
        });
    }

    protected override void ConfigureDefaults(GoogleOptions o)
    {
        o.ClientId = "whatever";
        o.ClientSecret = "whatever";
        o.SignInScheme = "auth1";
    }

    [Fact]
    public async Task ChallengeWillTriggerRedirection()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var location = transaction.Response.Headers.Location;

        Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth?", location.AbsoluteUri);

        var queryParams = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("code", queryParams["response_type"]);
        Assert.Equal("Test Id", queryParams["client_id"]);
        Assert.True(queryParams.ContainsKey("redirect_uri"));
        Assert.True(queryParams.ContainsKey("scope"));
        Assert.True(queryParams.ContainsKey("state"));
        Assert.True(queryParams.ContainsKey("code_challenge"));
        Assert.Equal("S256", queryParams["code_challenge_method"]);

        Assert.False(queryParams.ContainsKey("access_type"));
        Assert.False(queryParams.ContainsKey("prompt"));
        Assert.False(queryParams.ContainsKey("approval_prompt"));
        Assert.False(queryParams.ContainsKey("login_hint"));
        Assert.False(queryParams.ContainsKey("include_granted_scopes"));
    }

    [Fact]
    public async Task SignInThrows()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/signIn");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task SignOutThrows()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/signOut");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task ForbidThrows()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/signOut");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task Challenge401WillNotTriggerRedirection()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/401");
        Assert.Equal(HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task ChallengeWillSetCorrelationCookie()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge");
        Assert.Contains(transaction.SetCookie, cookie => cookie.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ChallengeWillSetDefaultScope()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var query = transaction.Response.Headers.Location.Query;
        Assert.Contains("&scope=" + UrlEncoder.Default.Encode("openid profile email"), query);
    }

    [Fact]
    public async Task ChallengeWillUseAuthenticationPropertiesParametersAsQueryArguments()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
        },
        context =>
        {
            var req = context.Request;
            var res = context.Response;
            if (req.Path == new PathString("/challenge2"))
            {
                return context.ChallengeAsync("Google", new GoogleChallengeProperties
                {
                    Scope = new string[] { "openid", "https://www.googleapis.com/auth/plus.login" },
                    AccessType = "offline",
                    ApprovalPrompt = "force",
                    Prompt = "consent",
                    LoginHint = "test@example.com",
                    IncludeGrantedScopes = false,
                });
            }

            return Task.FromResult<object>(null);
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge2");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        // verify query arguments
        var query = QueryHelpers.ParseQuery(transaction.Response.Headers.Location.Query);
        Assert.Equal("openid https://www.googleapis.com/auth/plus.login", query["scope"]);
        Assert.Equal("offline", query["access_type"]);
        Assert.Equal("force", query["approval_prompt"]);
        Assert.Equal("consent", query["prompt"]);
        Assert.Equal("false", query["include_granted_scopes"]);
        Assert.Equal("test@example.com", query["login_hint"]);

        // verify that the passed items were not serialized
        var stateProperties = stateFormat.Unprotect(query["state"]);
        Assert.DoesNotContain("scope", stateProperties.Items.Keys);
        Assert.DoesNotContain("access_type", stateProperties.Items.Keys);
        Assert.DoesNotContain("include_granted_scopes", stateProperties.Items.Keys);
        Assert.DoesNotContain("approval_prompt", stateProperties.Items.Keys);
        Assert.DoesNotContain("prompt", stateProperties.Items.Keys);
        Assert.DoesNotContain("login_hint", stateProperties.Items.Keys);
    }

    [Fact]
    public async Task ChallengeWillUseAuthenticationPropertiesItemsAsParameters()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
        },
        context =>
        {
            var req = context.Request;
            var res = context.Response;
            if (req.Path == new PathString("/challenge2"))
            {
                return context.ChallengeAsync("Google", new AuthenticationProperties(new Dictionary<string, string>()
                {
                        { "scope", "https://www.googleapis.com/auth/plus.login" },
                        { "access_type", "offline" },
                        { "approval_prompt", "force" },
                        { "prompt", "consent" },
                        { "login_hint", "test@example.com" },
                        { "include_granted_scopes", "false" }
                }));
            }

            return Task.FromResult<object>(null);
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge2");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        // verify query arguments
        var query = QueryHelpers.ParseQuery(transaction.Response.Headers.Location.Query);
        Assert.Equal("https://www.googleapis.com/auth/plus.login", query["scope"]);
        Assert.Equal("offline", query["access_type"]);
        Assert.Equal("force", query["approval_prompt"]);
        Assert.Equal("consent", query["prompt"]);
        Assert.Equal("false", query["include_granted_scopes"]);
        Assert.Equal("test@example.com", query["login_hint"]);

        // verify that the passed items were not serialized
        var stateProperties = stateFormat.Unprotect(query["state"]);
        Assert.DoesNotContain("scope", stateProperties.Items.Keys);
        Assert.DoesNotContain("access_type", stateProperties.Items.Keys);
        Assert.DoesNotContain("include_granted_scopes", stateProperties.Items.Keys);
        Assert.DoesNotContain("approval_prompt", stateProperties.Items.Keys);
        Assert.DoesNotContain("prompt", stateProperties.Items.Keys);
        Assert.DoesNotContain("login_hint", stateProperties.Items.Keys);
    }

    [Fact]
    public async Task ChallengeWillUseAuthenticationPropertiesItemsAsQueryArgumentsButParametersWillOverwrite()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
        },
        context =>
        {
            var req = context.Request;
            var res = context.Response;
            if (req.Path == new PathString("/challenge2"))
            {
                return context.ChallengeAsync("Google", new GoogleChallengeProperties(new Dictionary<string, string>
                {
                    ["scope"] = "https://www.googleapis.com/auth/plus.login",
                    ["access_type"] = "offline",
                    ["include_granted_scopes"] = "false",
                    ["approval_prompt"] = "force",
                    ["prompt"] = "login",
                    ["login_hint"] = "this-will-be-overwritten@example.com",
                })
                {
                    Prompt = "consent",
                    LoginHint = "test@example.com",
                });
            }

            return Task.FromResult<object>(null);
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge2");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        // verify query arguments
        var query = QueryHelpers.ParseQuery(transaction.Response.Headers.Location.Query);
        Assert.Equal("https://www.googleapis.com/auth/plus.login", query["scope"]);
        Assert.Equal("offline", query["access_type"]);
        Assert.Equal("force", query["approval_prompt"]);
        Assert.Equal("consent", query["prompt"]);
        Assert.Equal("false", query["include_granted_scopes"]);
        Assert.Equal("test@example.com", query["login_hint"]);

        // verify that the passed items were not serialized
        var stateProperties = stateFormat.Unprotect(query["state"]);
        Assert.DoesNotContain("scope", stateProperties.Items.Keys);
        Assert.DoesNotContain("access_type", stateProperties.Items.Keys);
        Assert.DoesNotContain("include_granted_scopes", stateProperties.Items.Keys);
        Assert.DoesNotContain("approval_prompt", stateProperties.Items.Keys);
        Assert.DoesNotContain("prompt", stateProperties.Items.Keys);
        Assert.DoesNotContain("login_hint", stateProperties.Items.Keys);
    }

    [Fact]
    public async Task ChallengeWillTriggerApplyRedirectEvent()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.Events = new OAuthEvents
            {
                OnRedirectToAuthorizationEndpoint = context =>
                {
                    context.Response.Redirect(context.RedirectUri + "&custom=test");
                    return Task.FromResult(0);
                }
            };
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var query = transaction.Response.Headers.Location.Query;
        Assert.Contains("custom=test", query);
    }

    [Fact]
    public async Task AuthenticateWithoutCookieWillReturnNoResult()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        },
        async context =>
        {
            var req = context.Request;
            var res = context.Response;
            if (req.Path == new PathString("/auth"))
            {
                var result = await context.AuthenticateAsync("Google");
                Assert.True(result.None);
            }
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/auth");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task ReplyPathWithoutStateQueryStringWillBeRejected()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });
        using var server = host.GetTestServer();
        var error = await Assert.ThrowsAnyAsync<Exception>(() => server.SendAsync("https://example.com/signin-google?code=TestCode"));
        Assert.Equal("The oauth state was missing or invalid.", error.GetBaseException().Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReplyPathWithAccessDeniedErrorFails(bool redirect)
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = new TestStateDataFormat();
            o.Events = redirect ? new OAuthEvents()
            {
                OnAccessDenied = ctx =>
                {
                    ctx.Response.Redirect("/error?FailureMessage=AccessDenied");
                    ctx.HandleResponse();
                    return Task.FromResult(0);
                }
            } : new OAuthEvents();
        });
        using var server = host.GetTestServer();
        var sendTask = server.SendAsync("https://example.com/signin-google?error=access_denied&error_description=SoBad&error_uri=foobar&state=protected_state",
            ".AspNetCore.Correlation.correlationId=N");
        if (redirect)
        {
            var transaction = await sendTask;
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/error?FailureMessage=AccessDenied", transaction.Response.Headers.GetValues("Location").First());
        }
        else
        {
            var error = await Assert.ThrowsAnyAsync<Exception>(() => sendTask);
            Assert.Equal("Access was denied by the resource owner or by the remote server.", error.GetBaseException().Message);
        }
    }

    [Fact]
    public async Task ReplyPathWithAccessDeniedError_AllowsCustomizingPath()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = new TestStateDataFormat();
            o.AccessDeniedPath = "/access-denied";
            o.Events = new OAuthEvents()
            {
                OnAccessDenied = ctx =>
                {
                    Assert.Equal("/access-denied", ctx.AccessDeniedPath.Value);
                    Assert.Equal("http://testhost/redirect", ctx.ReturnUrl);
                    Assert.Equal("ReturnUrl", ctx.ReturnUrlParameter);
                    ctx.AccessDeniedPath = "/custom-denied-page";
                    ctx.ReturnUrl = "http://www.google.com/";
                    ctx.ReturnUrlParameter = "rurl";
                    return Task.FromResult(0);
                }
            };
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/signin-google?error=access_denied&error_description=SoBad&error_uri=foobar&state=protected_state",
            ".AspNetCore.Correlation.correlationId=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("https://example.com/custom-denied-page?rurl=http%3A%2F%2Fwww.google.com%2F", transaction.Response.Headers.GetValues("Location").First());
    }

    [Fact]
    public async Task ReplyPathWithAccessDeniedErrorAndNoAccessDeniedPath_FallsBackToRemoteError()
    {
        var accessDeniedCalled = false;
        var remoteFailureCalled = false;
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = new TestStateDataFormat();
            o.Events = new OAuthEvents()
            {
                OnAccessDenied = ctx =>
                {
                    Assert.Null(ctx.AccessDeniedPath.Value);
                    Assert.Equal("http://testhost/redirect", ctx.ReturnUrl);
                    Assert.Equal("ReturnUrl", ctx.ReturnUrlParameter);
                    accessDeniedCalled = true;
                    return Task.FromResult(0);
                },
                OnRemoteFailure = ctx =>
                {
                    var ex = ctx.Failure;
                    Assert.True(ex.Data.Contains("error"), "error");
                    Assert.True(ex.Data.Contains("error_description"), "error_description");
                    Assert.True(ex.Data.Contains("error_uri"), "error_uri");
                    Assert.Equal("access_denied", ex.Data["error"]);
                    Assert.Equal("whyitfailed", ex.Data["error_description"]);
                    Assert.Equal("https://example.com/fail", ex.Data["error_uri"]);
                    remoteFailureCalled = true;
                    ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                    ctx.HandleResponse();
                    return Task.FromResult(0);
                }
            };
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/signin-google?error=access_denied&error_description=whyitfailed&error_uri=https://example.com/fail&state=protected_state",
            ".AspNetCore.Correlation.correlationId=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.StartsWith("/error?FailureMessage=", transaction.Response.Headers.GetValues("Location").First());
        Assert.True(accessDeniedCalled);
        Assert.True(remoteFailureCalled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReplyPathWithErrorFails(bool redirect)
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = new TestStateDataFormat();
            o.Events = redirect ? new OAuthEvents()
            {
                OnRemoteFailure = ctx =>
                {
                    var ex = ctx.Failure;
                    Assert.True(ex.Data.Contains("error"), "error");
                    Assert.True(ex.Data.Contains("error_description"), "error_description");
                    Assert.True(ex.Data.Contains("error_uri"), "error_uri");
                    Assert.Equal("itfailed", ex.Data["error"]);
                    Assert.Equal("whyitfailed", ex.Data["error_description"]);
                    Assert.Equal("https://example.com/fail", ex.Data["error_uri"]);
                    ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                    ctx.HandleResponse();
                    return Task.FromResult(0);
                }
            } : new OAuthEvents();
        });
        using var server = host.GetTestServer();
        var sendTask = server.SendAsync("https://example.com/signin-google?error=itfailed&error_description=whyitfailed&error_uri=https://example.com/fail&state=protected_state",
            ".AspNetCore.Correlation.correlationId=N");
        if (redirect)
        {
            var transaction = await sendTask;
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/error?FailureMessage=itfailed" + UrlEncoder.Default.Encode(";Description=whyitfailed;Uri=https://example.com/fail"), transaction.Response.Headers.GetValues("Location").First());
        }
        else
        {
            var error = await Assert.ThrowsAnyAsync<Exception>(() => sendTask);
            Assert.Equal("itfailed;Description=whyitfailed;Uri=https://example.com/fail", error.GetBaseException().Message);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("CustomIssuer")]
    public async Task ReplyPathWillAuthenticateValidAuthorizeCodeAndState(string claimsIssuer)
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.SaveTokens = true;
            o.StateDataFormat = stateFormat;
            if (claimsIssuer != null)
            {
                o.ClaimsIssuer = claimsIssuer;
            }
            o.BackchannelHttpHandler = CreateBackchannel();
        });

        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.Contains($".AspNetCore.Correlation.{correlationValue}", transaction.SetCookie[0]);
        Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

        var authCookie = transaction.AuthenticationCookieValue;
        transaction = await server.SendAsync("https://example.com/me", authCookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        var expectedIssuer = claimsIssuer ?? GoogleDefaults.AuthenticationScheme;
        Assert.Equal("Test Name", transaction.FindClaimValue(ClaimTypes.Name, expectedIssuer));
        Assert.Equal("Test User ID", transaction.FindClaimValue(ClaimTypes.NameIdentifier, expectedIssuer));
        Assert.Equal("Test Given Name", transaction.FindClaimValue(ClaimTypes.GivenName, expectedIssuer));
        Assert.Equal("Test Family Name", transaction.FindClaimValue(ClaimTypes.Surname, expectedIssuer));
        Assert.Equal("Test email", transaction.FindClaimValue(ClaimTypes.Email, expectedIssuer));

        // Ensure claims transformation
        Assert.Equal("yup", transaction.FindClaimValue("xform"));

        transaction = await server.SendAsync("https://example.com/tokens", authCookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Equal("Test Access Token", transaction.FindTokenValue("access_token"));
        Assert.Equal("Bearer", transaction.FindTokenValue("token_type"));
        Assert.NotNull(transaction.FindTokenValue("expires_at"));
    }

    // REVIEW: Fix this once we revisit error handling to not blow up
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReplyPathWillThrowIfCodeIsInvalid(bool redirect)
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.BackchannelHttpHandler = new TestHttpMessageHandler
            {
                Sender = req =>
                {
                    return ReturnJsonResponse(new { Error = "Error" },
                        HttpStatusCode.BadRequest);
                }
            };
            o.Events = redirect ? new OAuthEvents()
            {
                OnRemoteFailure = ctx =>
                {
                    ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                    ctx.HandleResponse();
                    return Task.FromResult(0);
                }
            } : new OAuthEvents();
        });
        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";

        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var sendTask = server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        if (redirect)
        {
            var transaction = await sendTask;
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/error?FailureMessage=" + UrlEncoder.Default.Encode("OAuth token endpoint failure: Status: BadRequest;Headers: ;Body: {\"Error\":\"Error\"};"),
                transaction.Response.Headers.GetValues("Location").First());
        }
        else
        {
            var error = await Assert.ThrowsAnyAsync<Exception>(() => sendTask);
            Assert.Equal("OAuth token endpoint failure: Status: BadRequest;Headers: ;Body: {\"Error\":\"Error\"};", error.GetBaseException().Message);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReplyPathWillRejectIfAccessTokenIsMissing(bool redirect)
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.BackchannelHttpHandler = new TestHttpMessageHandler
            {
                Sender = req =>
                {
                    return ReturnJsonResponse(new object());
                }
            };
            o.Events = redirect ? new OAuthEvents()
            {
                OnRemoteFailure = ctx =>
                {
                    ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                    ctx.HandleResponse();
                    return Task.FromResult(0);
                }
            } : new OAuthEvents();
        });
        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var sendTask = server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        if (redirect)
        {
            var transaction = await sendTask;
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/error?FailureMessage=" + UrlEncoder.Default.Encode("Failed to retrieve access token."),
                transaction.Response.Headers.GetValues("Location").First());
        }
        else
        {
            var error = await Assert.ThrowsAnyAsync<Exception>(() => sendTask);
            Assert.Equal("Failed to retrieve access token.", error.GetBaseException().Message);
        }
    }

    [Fact]
    public async Task AuthenticatedEventCanGetRefreshToken()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.BackchannelHttpHandler = CreateBackchannel();
            o.Events = new OAuthEvents
            {
                OnCreatingTicket = context =>
                {
                    var refreshToken = context.RefreshToken;
                    context.Principal.AddIdentity(new ClaimsIdentity(new Claim[] { new Claim("RefreshToken", refreshToken, ClaimValueTypes.String, "Google") }, "Google"));
                    return Task.FromResult(0);
                }
            };
        });
        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.Contains($".AspNetCore.Correlation.{correlationValue}", transaction.SetCookie[0]);
        Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

        var authCookie = transaction.AuthenticationCookieValue;
        transaction = await server.SendAsync("https://example.com/me", authCookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Equal("Test Refresh Token", transaction.FindClaimValue("RefreshToken"));
    }

    [Fact]
    public async Task NullRedirectUriWillRedirectToSlash()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.BackchannelHttpHandler = CreateBackchannel();
            o.Events = new OAuthEvents
            {
                OnTicketReceived = context =>
                {
                    context.Properties.RedirectUri = null;
                    return Task.FromResult(0);
                }
            };
        });
        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.Contains($".AspNetCore.Correlation.{correlationValue}", transaction.SetCookie[0]);
        Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);
    }

    [Fact]
    public async Task ValidateAuthenticatedContext()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.AccessType = "offline";
            o.Events = new OAuthEvents()
            {
                OnCreatingTicket = context =>
                {
                    Assert.Equal("Test Access Token", context.AccessToken);
                    Assert.Equal("Test Refresh Token", context.RefreshToken);
                    Assert.Equal(TimeSpan.FromSeconds(3600), context.ExpiresIn);
                    Assert.Equal("Test email", context.Identity.FindFirst(ClaimTypes.Email)?.Value);
                    Assert.Equal("Test User ID", context.Identity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    Assert.Equal("Test Name", context.Identity.FindFirst(ClaimTypes.Name)?.Value);
                    Assert.Equal("Test Family Name", context.Identity.FindFirst(ClaimTypes.Surname)?.Value);
                    Assert.Equal("Test Given Name", context.Identity.FindFirst(ClaimTypes.GivenName)?.Value);
                    return Task.FromResult(0);
                }
            };
            o.BackchannelHttpHandler = CreateBackchannel();
        });

        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/foo";
        var state = stateFormat.Protect(properties);

        //Post a message to the Google middleware
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/foo", transaction.Response.Headers.GetValues("Location").First());
    }

    [Fact]
    public async Task NoStateCausesException()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
        });

        //Post a message to the Google middleware
        using var server = host.GetTestServer();
        var error = await Assert.ThrowsAnyAsync<Exception>(() => server.SendAsync("https://example.com/signin-google?code=TestCode"));
        Assert.Equal("The oauth state was missing or invalid.", error.GetBaseException().Message);
    }

    [Fact]
    public async Task CanRedirectOnError()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.Events = new OAuthEvents()
            {
                OnRemoteFailure = ctx =>
                {
                    ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                    ctx.HandleResponse();
                    return Task.FromResult(0);
                }
            };
        });

        //Post a message to the Google middleware
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/error?FailureMessage=" + UrlEncoder.Default.Encode("The oauth state was missing or invalid."),
            transaction.Response.Headers.GetValues("Location").First());
    }

    [Fact]
    public async Task AuthenticateAutomaticWhenAlreadySignedInSucceeds()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.SaveTokens = true;
            o.BackchannelHttpHandler = CreateBackchannel();
        });

        // Skip the challenge step, go directly to the callback path

        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.Contains($".AspNetCore.Correlation.{correlationValue}", transaction.SetCookie[0]); // Delete
        Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

        var authCookie = transaction.AuthenticationCookieValue;
        transaction = await server.SendAsync("https://example.com/authenticate", authCookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Equal("Test Name", transaction.FindClaimValue(ClaimTypes.Name));
        Assert.Equal("Test User ID", transaction.FindClaimValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Test Given Name", transaction.FindClaimValue(ClaimTypes.GivenName));
        Assert.Equal("Test Family Name", transaction.FindClaimValue(ClaimTypes.Surname));
        Assert.Equal("Test email", transaction.FindClaimValue(ClaimTypes.Email));

        // Ensure claims transformation
        Assert.Equal("yup", transaction.FindClaimValue("xform"));
    }

    [Fact]
    public async Task AuthenticateGoogleWhenAlreadySignedInSucceeds()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.SaveTokens = true;
            o.BackchannelHttpHandler = CreateBackchannel();
        });

        // Skip the challenge step, go directly to the callback path

        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.Contains($".AspNetCore.Correlation.{correlationValue}", transaction.SetCookie[0]); // Delete
        Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

        var authCookie = transaction.AuthenticationCookieValue;
        transaction = await server.SendAsync("https://example.com/authenticateGoogle", authCookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Equal("Test Name", transaction.FindClaimValue(ClaimTypes.Name));
        Assert.Equal("Test User ID", transaction.FindClaimValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Test Given Name", transaction.FindClaimValue(ClaimTypes.GivenName));
        Assert.Equal("Test Family Name", transaction.FindClaimValue(ClaimTypes.Surname));
        Assert.Equal("Test email", transaction.FindClaimValue(ClaimTypes.Email));

        // Ensure claims transformation
        Assert.Equal("yup", transaction.FindClaimValue("xform"));
    }

    [Fact]
    public async Task AuthenticateGoogleWhenAlreadySignedWithGoogleReturnsNull()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.SaveTokens = true;
            o.BackchannelHttpHandler = CreateBackchannel();
        });

        // Skip the challenge step, go directly to the callback path

        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.Contains($".AspNetCore.Correlation.{correlationValue}", transaction.SetCookie[0]); // Delete
        Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

        var authCookie = transaction.AuthenticationCookieValue;
        transaction = await server.SendAsync("https://example.com/authenticateFacebook", authCookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Null(transaction.FindClaimValue(ClaimTypes.Name));
    }

    [Fact]
    public async Task ChallengeGoogleWhenAlreadySignedWithGoogleSucceeds()
    {
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Id";
            o.ClientSecret = "Test Secret";
            o.StateDataFormat = stateFormat;
            o.SaveTokens = true;
            o.BackchannelHttpHandler = CreateBackchannel();
        });

        // Skip the challenge step, go directly to the callback path

        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.Contains($".AspNetCore.Correlation.{correlationValue}", transaction.SetCookie[0]); // Delete
        Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

        var authCookie = transaction.AuthenticationCookieValue;
        transaction = await server.SendAsync("https://example.com/challengeFacebook", authCookie);
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.StartsWith("https://www.facebook.com/", transaction.Response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task PkceSentToTokenEndpoint()
    {
        using var host = await CreateHost(o =>
        {
            o.ClientId = "Test Client Id";
            o.ClientSecret = "Test Client Secret";
            o.BackchannelHttpHandler = new TestHttpMessageHandler
            {
                Sender = req =>
                {
                    if (req.RequestUri.AbsoluteUri == "https://oauth2.googleapis.com/token")
                    {
                        var body = req.Content.ReadAsStringAsync().Result;
                        var form = new FormReader(body);
                        var entries = form.ReadForm();
                        Assert.Equal("Test Client Id", entries["client_id"]);
                        Assert.Equal("https://example.com/signin-google", entries["redirect_uri"]);
                        Assert.Equal("Test Client Secret", entries["client_secret"]);
                        Assert.Equal("TestCode", entries["code"]);
                        Assert.Equal("authorization_code", entries["grant_type"]);
                        Assert.False(string.IsNullOrEmpty(entries["code_verifier"]));

                        return ReturnJsonResponse(new
                        {
                            access_token = "Test Access Token",
                            expire_in = 3600,
                            token_type = "Bearer",
                        });
                    }
                    else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://www.googleapis.com/oauth2/v3/userinfo")
                    {
                        return ReturnJsonResponse(new
                        {
                            id = "Test User ID",
                            displayName = "Test Name",
                            givenName = "Test Given Name",
                            surname = "Test Family Name",
                            mail = "Test email"
                        });
                    }

                    return null;
                }
            };
        });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var locationUri = transaction.Response.Headers.Location;
        Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth", locationUri.AbsoluteUri);

        var queryParams = QueryHelpers.ParseQuery(locationUri.Query);
        Assert.False(string.IsNullOrEmpty(queryParams["code_challenge"]));
        Assert.Equal("S256", queryParams["code_challenge_method"]);

        var nonceCookie = transaction.SetCookie.Single();
        nonceCookie = nonceCookie.Substring(0, nonceCookie.IndexOf(';'));

        transaction = await server.SendAsync(
            "https://example.com/signin-google?code=TestCode&state=" + queryParams["state"],
            nonceCookie);
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/challenge", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.StartsWith(".AspNetCore.Correlation.", transaction.SetCookie[0]);
        Assert.StartsWith(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);
    }

    private HttpMessageHandler CreateBackchannel()
    {
        return new TestHttpMessageHandler()
        {
            Sender = req =>
            {
                if (req.RequestUri.AbsoluteUri == "https://oauth2.googleapis.com/token")
                {
                    return ReturnJsonResponse(new
                    {
                        access_token = "Test Access Token",
                        expires_in = 3600,
                        token_type = "Bearer",
                        refresh_token = "Test Refresh Token"
                    });
                }
                else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://www.googleapis.com/oauth2/v3/userinfo")
                {
                    return ReturnJsonResponse(new
                    {
                        sub = "Test User ID",
                        name = "Test Name",
                        given_name = "Test Given Name",
                        family_name = "Test Family Name",
                        link = "Profile link",
                        email = "Test email",
                    });
                }

                throw new NotImplementedException(req.RequestUri.AbsoluteUri);
            }
        };
    }

    private static HttpResponseMessage ReturnJsonResponse(object content, HttpStatusCode code = HttpStatusCode.OK)
    {
        var res = new HttpResponseMessage(code);
        var text = Newtonsoft.Json.JsonConvert.SerializeObject(content);
        res.Content = new StringContent(text, Encoding.UTF8, "application/json");
        return res;
    }

    private class ClaimsTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal p)
        {
            if (!p.Identities.Any(i => i.AuthenticationType == "xform"))
            {
                var id = new ClaimsIdentity("xform");
                id.AddClaim(new Claim("xform", "yup"));
                p.AddIdentity(id);
            }
            return Task.FromResult(p);
        }
    }

    private static async Task<IHost> CreateHost(Action<GoogleOptions> configureOptions, Func<HttpContext, Task> testpath = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Use(async (context, next) =>
                        {
                            var req = context.Request;
                            var res = context.Response;
                            if (req.Path == new PathString("/challenge"))
                            {
                                await context.ChallengeAsync();
                            }
                            else if (req.Path == new PathString("/challengeFacebook"))
                            {
                                await context.ChallengeAsync("Facebook");
                            }
                            else if (req.Path == new PathString("/tokens"))
                            {
                                var result = await context.AuthenticateAsync(TestExtensions.CookieAuthenticationScheme);
                                var tokens = result.Properties.GetTokens();
                                await res.DescribeAsync(tokens);
                            }
                            else if (req.Path == new PathString("/me"))
                            {
                                await res.DescribeAsync(context.User);
                            }
                            else if (req.Path == new PathString("/authenticate"))
                            {
                                var result = await context.AuthenticateAsync(TestExtensions.CookieAuthenticationScheme);
                                await res.DescribeAsync(result.Principal);
                            }
                            else if (req.Path == new PathString("/authenticateGoogle"))
                            {
                                var result = await context.AuthenticateAsync("Google");
                                await res.DescribeAsync(result?.Principal);
                            }
                            else if (req.Path == new PathString("/authenticateFacebook"))
                            {
                                var result = await context.AuthenticateAsync("Facebook");
                                await res.DescribeAsync(result?.Principal);
                            }
                            else if (req.Path == new PathString("/unauthorized"))
                            {
                                // Simulate Authorization failure
                                var result = await context.AuthenticateAsync("Google");
                                await context.ChallengeAsync("Google");
                            }
                            else if (req.Path == new PathString("/unauthorizedAuto"))
                            {
                                var result = await context.AuthenticateAsync("Google");
                                await context.ChallengeAsync("Google");
                            }
                            else if (req.Path == new PathString("/401"))
                            {
                                res.StatusCode = 401;
                            }
                            else if (req.Path == new PathString("/signIn"))
                            {
                                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync("Google", new ClaimsPrincipal()));
                            }
                            else if (req.Path == new PathString("/signOut"))
                            {
                                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync("Google"));
                            }
                            else if (req.Path == new PathString("/forbid"))
                            {
                                await Assert.ThrowsAsync<InvalidOperationException>(() => context.ForbidAsync("Google"));
                            }
                            else if (testpath != null)
                            {
                                await testpath(context);
                            }
                            else
                            {
                                await next(context);
                            }
                        });
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
                        services.AddAuthentication(TestExtensions.CookieAuthenticationScheme)
                            .AddCookie(TestExtensions.CookieAuthenticationScheme, o => o.ForwardChallenge = GoogleDefaults.AuthenticationScheme)
                            .AddGoogle(configureOptions)
                            .AddFacebook(o =>
                            {
                                o.ClientId = "Test ClientId";
                                o.ClientSecret = "Test AppSecrent";
                            });
                    }))
                .Build();

        await host.StartAsync();
        return host;
    }

    private class TestStateDataFormat : ISecureDataFormat<AuthenticationProperties>
    {
        private AuthenticationProperties Data { get; set; }

        public string Protect(AuthenticationProperties data)
        {
            return "protected_state";
        }

        public string Protect(AuthenticationProperties data, string purpose)
        {
            throw new NotImplementedException();
        }

        public AuthenticationProperties Unprotect(string protectedText)
        {
            Assert.Equal("protected_state", protectedText);
            var properties = new AuthenticationProperties(new Dictionary<string, string>()
                {
                    { ".xsrf", "correlationId" },
                    { "testkey", "testvalue" }
                });
            properties.RedirectUri = "http://testhost/redirect";
            return properties;
        }

        public AuthenticationProperties Unprotect(string protectedText, string purpose)
        {
            throw new NotImplementedException();
        }
    }
}
