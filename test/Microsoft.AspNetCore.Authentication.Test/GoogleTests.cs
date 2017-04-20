// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Google
{
    public class GoogleTests
    {
        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection().AddGoogleAuthentication().AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(GoogleDefaults.AuthenticationScheme);
            Assert.NotNull(scheme);
            Assert.Equal("GoogleHandler", scheme.HandlerType.Name);
            Assert.Equal(GoogleDefaults.AuthenticationScheme, scheme.DisplayName);
        }

        [Fact]
        public void AddCanBindAgainstDefaultConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"Google:ClientId", "<id>"},
                {"Google:ClientSecret", "<secret>"},
                {"Google:AuthorizationEndpoint", "<authEndpoint>"},
                {"Google:BackchannelTimeout", "0.0:0:30"},
                //{"Google:CallbackPath", "/callbackpath"}, // PathString doesn't convert
                {"Google:ClaimsIssuer", "<issuer>"},
                {"Google:DisplayName", "<display>"},
                {"Google:RemoteAuthenticationTimeout", "0.0:0:30"},
                {"Google:SaveTokens", "true"},
                {"Google:SendAppSecretProof", "true"},
                {"Google:SignInScheme", "<signIn>"},
                {"Google:TokenEndpoint", "<tokenEndpoint>"},
                {"Google:UserInformationEndpoint", "<userEndpoint>"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection().AddGoogleAuthentication().AddSingleton<IConfiguration>(config);
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptionsSnapshot<GoogleOptions>>().Get(GoogleDefaults.AuthenticationScheme);
            Assert.Equal("<authEndpoint>", options.AuthorizationEndpoint);
            Assert.Equal(new TimeSpan(0, 0, 0, 30), options.BackchannelTimeout);
            //Assert.Equal("/callbackpath", options.CallbackPath); // NOTE: PathString doesn't convert
            Assert.Equal("<issuer>", options.ClaimsIssuer);
            Assert.Equal("<id>", options.ClientId);
            Assert.Equal("<secret>", options.ClientSecret);
            Assert.Equal("<display>", options.DisplayName);
            Assert.Equal(new TimeSpan(0, 0, 0, 30), options.RemoteAuthenticationTimeout);
            Assert.True(options.SaveTokens);
            Assert.Equal("<signIn>", options.SignInScheme);
            Assert.Equal("<tokenEndpoint>", options.TokenEndpoint);
            Assert.Equal("<userEndpoint>", options.UserInformationEndpoint);
        }

        [Fact]
        public void AddWithDelegateIgnoresConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"Google:ClientId", "<id>"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection().AddGoogleAuthentication(o => o.SaveTokens = false).AddSingleton<IConfiguration>(config);
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptionsSnapshot<GoogleOptions>>().Get(GoogleDefaults.AuthenticationScheme);
            Assert.Null(options.ClientId);
            Assert.False(options.SaveTokens);
        }

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location.ToString();
            Assert.Contains("https://accounts.google.com/o/oauth2/auth?response_type=code", location);
            Assert.Contains("&client_id=", location);
            Assert.Contains("&redirect_uri=", location);
            Assert.Contains("&scope=", location);
            Assert.Contains("&state=", location);

            Assert.DoesNotContain("access_type=", location);
            Assert.DoesNotContain("prompt=", location);
            Assert.DoesNotContain("approval_prompt=", location);
            Assert.DoesNotContain("login_hint=", location);
            Assert.DoesNotContain("include_granted_scopes=", location);
        }

        [Fact]
        public async Task SignInThrows()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signIn");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task SignOutThrows()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ForbidThrows()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task Challenge401WillNotTriggerRedirection()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/401");
            Assert.Equal(HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ChallengeWillSetCorrelationCookie()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            Assert.Contains(transaction.SetCookie, cookie => cookie.StartsWith(".AspNetCore.Correlation.Google."));
        }

        [Fact]
        public async Task ChallengeWillSetDefaultScope()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var query = transaction.Response.Headers.Location.Query;
            Assert.Contains("&scope=" + UrlEncoder.Default.Encode("openid profile email"), query);
        }

        [Fact]
        public async Task ChallengeWillUseAuthenticationPropertiesAsParameters()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                //AutomaticChallenge = true
            },
            context =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    if (req.Path == new PathString("/challenge2"))
                    {
                        return context.ChallengeAsync("Google", new AuthenticationProperties(
                            new Dictionary<string, string>()
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
            var transaction = await server.SendAsync("https://example.com/challenge2");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var query = transaction.Response.Headers.Location.Query;
            Assert.Contains("scope=" + UrlEncoder.Default.Encode("https://www.googleapis.com/auth/plus.login"), query);
            Assert.Contains("access_type=offline", query);
            Assert.Contains("approval_prompt=force", query);
            Assert.Contains("prompt=consent", query);
            Assert.Contains("include_granted_scopes=false", query);
            Assert.Contains("login_hint=" + UrlEncoder.Default.Encode("test@example.com"), query);
        }

        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(o =>
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
            var transaction = await server.SendAsync("https://example.com/challenge");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var query = transaction.Response.Headers.Location.Query;
            Assert.Contains("custom=test", query);
        }

        [Fact]
        public async Task AuthenticateWithoutCookieWillFail()
        {
            var server = CreateServer(o =>
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
                    Assert.NotNull(result.Failure);
                }
            });
            var transaction = await server.SendAsync("https://example.com/auth");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task ReplyPathWithoutStateQueryStringWillBeRejected()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });
            var error = await Assert.ThrowsAnyAsync<Exception>(() => server.SendAsync("https://example.com/signin-google?code=TestCode"));
            Assert.Equal("The oauth state was missing or invalid.", error.GetBaseException().Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReplyPathWithErrorFails(bool redirect)
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
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
            var sendTask = server.SendAsync("https://example.com/signin-google?error=OMG&error_description=SoBad&error_uri=foobar");
            if (redirect)
            {
                var transaction = await sendTask;
                Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
                Assert.Equal("/error?FailureMessage=OMG" + UrlEncoder.Default.Encode(";Description=SoBad;Uri=foobar"), transaction.Response.Headers.GetValues("Location").First());
            }
            else
            {
                var error = await Assert.ThrowsAnyAsync<Exception>(() => sendTask);
                Assert.Equal("OMG;Description=SoBad;Uri=foobar", error.GetBaseException().Message);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("CustomIssuer")]
        public async Task ReplyPathWillAuthenticateValidAuthorizeCodeAndState(string claimsIssuer)
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.SaveTokens = true;
                o.StateDataFormat = stateFormat;
                if (claimsIssuer != null)
                {
                    o.ClaimsIssuer = claimsIssuer;
                }
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri == "https://www.googleapis.com/oauth2/v4/token")
                        {
                            return ReturnJsonResponse(new
                            {
                                access_token = "Test Access Token",
                                expires_in = 3600,
                                token_type = "Bearer"
                            });
                        }
                        else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://www.googleapis.com/plus/v1/people/me")
                        {
                            return ReturnJsonResponse(new
                            {
                                id = "Test User ID",
                                displayName = "Test Name",
                                name = new
                                {
                                    familyName = "Test Family Name",
                                    givenName = "Test Given Name"
                                },
                                url = "Profile link",
                                emails = new[]
                                {
                                    new
                                    {
                                        value = "Test email",
                                        type = "account"
                                    }
                                }
                            });
                        }

                        throw new NotImplementedException(req.RequestUri.AbsoluteUri);
                    }
                };
            });

            var properties = new AuthenticationProperties();
            var correlationKey = ".xsrf";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Google.{correlationValue}", transaction.SetCookie[0]);
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
            var server = CreateServer(o =>
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
            var sendTask = server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
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
            var server = CreateServer(o =>
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
            var sendTask = server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
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
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.StateDataFormat = stateFormat;
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri == "https://www.googleapis.com/oauth2/v4/token")
                        {
                            return ReturnJsonResponse(new
                            {
                                access_token = "Test Access Token",
                                expires_in = 3600,
                                token_type = "Bearer",
                                refresh_token = "Test Refresh Token"
                            });
                        }
                        else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://www.googleapis.com/plus/v1/people/me")
                        {
                            return ReturnJsonResponse(new
                            {
                                id = "Test User ID",
                                displayName = "Test Name",
                                name = new
                                {
                                    familyName = "Test Family Name",
                                    givenName = "Test Given Name"
                                },
                                url = "Profile link",
                                emails = new[]
                                    {
                                        new
                                        {
                                            value = "Test email",
                                            type = "account"
                                        }
                                    }
                            });
                        }

                        throw new NotImplementedException(req.RequestUri.AbsoluteUri);
                    }
                };
                o.Events = new OAuthEvents
                {
                    OnCreatingTicket = context =>
                    {
                        var refreshToken = context.RefreshToken;
                        context.Ticket.Principal.AddIdentity(new ClaimsIdentity(new Claim[] { new Claim("RefreshToken", refreshToken, ClaimValueTypes.String, "Google") }, "Google"));
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
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Google.{correlationValue}", transaction.SetCookie[0]);
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
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.StateDataFormat = stateFormat;
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri == "https://www.googleapis.com/oauth2/v4/token")
                        {
                            return ReturnJsonResponse(new
                            {
                                access_token = "Test Access Token",
                                expires_in = 3600,
                                token_type = "Bearer",
                                refresh_token = "Test Refresh Token"
                            });
                        }
                        else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://www.googleapis.com/plus/v1/people/me")
                        {
                            return ReturnJsonResponse(new
                            {
                                id = "Test User ID",
                                displayName = "Test Name",
                                name = new
                                {
                                    familyName = "Test Family Name",
                                    givenName = "Test Given Name"
                                },
                                url = "Profile link",
                                emails = new[]
                                    {
                                        new
                                        {
                                            value = "Test email",
                                            type = "account"
                                        }
                                    }
                            });
                        }

                        throw new NotImplementedException(req.RequestUri.AbsoluteUri);
                    }
                };
                o.Events = new OAuthEvents
                {
                    OnTicketReceived = context =>
                    {
                        context.Ticket.Properties.RedirectUri = null;
                        return Task.FromResult(0);
                    }
                };
            });
            var properties = new AuthenticationProperties();
            var correlationKey = ".xsrf";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Google.{correlationValue}", transaction.SetCookie[0]);
            Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);
        }

        [Fact]
        public async Task ValidateAuthenticatedContext()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
                o.StateDataFormat = stateFormat;
                o.AccessType = "offline";
                o.Events = new OAuthEvents()
                {
                    OnCreatingTicket = context =>
                    {
                        Assert.NotNull(context.User);
                        Assert.Equal(context.AccessToken, "Test Access Token");
                        Assert.Equal(context.RefreshToken, "Test Refresh Token");
                        Assert.Equal(context.ExpiresIn, TimeSpan.FromSeconds(3600));
                        Assert.Equal(context.Identity.FindFirst(ClaimTypes.Email)?.Value, "Test email");
                        Assert.Equal(context.Identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Test User ID");
                        Assert.Equal(context.Identity.FindFirst(ClaimTypes.Name)?.Value, "Test Name");
                        Assert.Equal(context.Identity.FindFirst(ClaimTypes.Surname)?.Value, "Test Family Name");
                        Assert.Equal(context.Identity.FindFirst(ClaimTypes.GivenName)?.Value, "Test Given Name");
                        return Task.FromResult(0);
                    }
                };
                o.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri == "https://www.googleapis.com/oauth2/v4/token")
                        {
                            return ReturnJsonResponse(new
                            {
                                access_token = "Test Access Token",
                                expires_in = 3600,
                                token_type = "Bearer",
                                refresh_token = "Test Refresh Token"
                            });
                        }
                        else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://www.googleapis.com/plus/v1/people/me")
                        {
                            return ReturnJsonResponse(new
                            {
                                id = "Test User ID",
                                displayName = "Test Name",
                                name = new
                                {
                                    familyName = "Test Family Name",
                                    givenName = "Test Given Name"
                                },
                                url = "Profile link",
                                emails = new[]
                                    {
                                        new
                                        {
                                            value = "Test email",
                                            type = "account"
                                        }
                                    }
                            });
                        }

                        throw new NotImplementedException(req.RequestUri.AbsoluteUri);
                    }
                };
            });

            var properties = new AuthenticationProperties();
            var correlationKey = ".xsrf";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/foo";
            var state = stateFormat.Protect(properties);

            //Post a message to the Google middleware
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/foo", transaction.Response.Headers.GetValues("Location").First());
        }

        [Fact]
        public async Task NoStateCausesException()
        {
            var server = CreateServer(o =>
            {
                o.ClientId = "Test Id";
                o.ClientSecret = "Test Secret";
            });

            //Post a message to the Google middleware
            var error = await Assert.ThrowsAnyAsync<Exception>(() => server.SendAsync("https://example.com/signin-google?code=TestCode"));
            Assert.Equal("The oauth state was missing or invalid.", error.GetBaseException().Message);
        }

        [Fact]
        public async Task CanRedirectOnError()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
            var server = CreateServer(o =>
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
            var server = CreateServer(o =>
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
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Google.{correlationValue}", transaction.SetCookie[0]); // Delete
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
            var server = CreateServer(o =>
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
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Google.{correlationValue}", transaction.SetCookie[0]); // Delete
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
        public async Task ChallengeGoogleWhenAlreadySignedInReturnsForbidden()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
            var server = CreateServer(o =>
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
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Google.{correlationValue}", transaction.SetCookie[0]); // Delete
            Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await server.SendAsync("https://example.com/challenge", authCookie);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.StartsWith("https://example.com/Account/AccessDenied?", transaction.Response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task AuthenticateFacebookWhenAlreadySignedWithGoogleReturnsNull()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
            var server = CreateServer(o =>
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
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Google.{correlationValue}", transaction.SetCookie[0]); // Delete
            Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await server.SendAsync("https://example.com/authenticateFacebook", authCookie);
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.Equal(null, transaction.FindClaimValue(ClaimTypes.Name));
        }

        [Fact]
        public async Task ChallengeFacebookWhenAlreadySignedWithGoogleSucceeds()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("GoogleTest"));
            var server = CreateServer(o =>
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
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
                $".AspNetCore.Correlation.Google.{correlationValue}=N");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
            Assert.Equal(2, transaction.SetCookie.Count);
            Assert.Contains($".AspNetCore.Correlation.Google.{correlationValue}", transaction.SetCookie[0]); // Delete
            Assert.Contains(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await server.SendAsync("https://example.com/challengeFacebook", authCookie);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.StartsWith("https://www.facebook.com/", transaction.Response.Headers.Location.OriginalString);
        }

        private HttpMessageHandler CreateBackchannel()
        {
            return new TestHttpMessageHandler()
            {
                Sender = req =>
                {
                    if (req.RequestUri.AbsoluteUri == "https://www.googleapis.com/oauth2/v4/token")
                    {
                        return ReturnJsonResponse(new
                        {
                            access_token = "Test Access Token",
                            expires_in = 3600,
                            token_type = "Bearer"
                        });
                    }
                    else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://www.googleapis.com/plus/v1/people/me")
                    {
                        return ReturnJsonResponse(new
                        {
                            id = "Test User ID",
                            displayName = "Test Name",
                            name = new
                            {
                                familyName = "Test Family Name",
                                givenName = "Test Given Name"
                            },
                            url = "Profile link",
                            emails = new[]
                            {
                                new
                                {
                                    value = "Test email",
                                    type = "account"
                                }
                            }
                        });
                    }

                    throw new NotImplementedException(req.RequestUri.AbsoluteUri);
                }
            };
        }

        private static HttpResponseMessage ReturnJsonResponse(object content, HttpStatusCode code = HttpStatusCode.OK)
        {
            var res = new HttpResponseMessage(code);
            var text = JsonConvert.SerializeObject(content);
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

        private static TestServer CreateServer(Action<GoogleOptions> configureOptions, Func<HttpContext, Task> testpath = null)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        var req = context.Request;
                        var res = context.Response;
                        if (req.Path == new PathString("/challenge"))
                        {
                            await context.ChallengeAsync("Google");
                        }
                        else if (req.Path == new PathString("/challengeFacebook"))
                        {
                            await context.ChallengeAsync("Facebook");
                        }
                        else if (req.Path == new PathString("/tokens"))
                        {
                            var result = await context.AuthenticateAsync(TestExtensions.CookieAuthenticationScheme);
                            var tokens = result.Ticket.Properties.GetTokens();
                            res.Describe(tokens);
                        }
                        else if (req.Path == new PathString("/me"))
                        {
                            res.Describe(context.User);
                        }
                        else if (req.Path == new PathString("/authenticate"))
                        {
                            var result = await context.AuthenticateAsync(TestExtensions.CookieAuthenticationScheme);
                            res.Describe(result.Ticket.Principal);
                        }
                        else if (req.Path == new PathString("/authenticateGoogle"))
                        {
                            var result = await context.AuthenticateAsync("Google");
                            res.Describe(result?.Ticket?.Principal);
                        }
                        else if (req.Path == new PathString("/authenticateFacebook"))
                        {
                            var result = await context.AuthenticateAsync("Facebook");
                            res.Describe(result?.Ticket?.Principal);
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
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.SignInAsync("Google", new ClaimsPrincipal()));
                        }
                        else if (req.Path == new PathString("/signOut"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.SignOutAsync("Google"));
                        }
                        else if (req.Path == new PathString("/forbid"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.ForbidAsync("Google"));
                        }
                        else if (testpath != null)
                        {
                            await testpath(context);
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
                    services.AddAuthentication(o =>
                    {
                        o.DefaultAuthenticateScheme = TestExtensions.CookieAuthenticationScheme;
                        o.DefaultSignInScheme = TestExtensions.CookieAuthenticationScheme;
                        o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                    });
                    services.AddCookieAuthentication(TestExtensions.CookieAuthenticationScheme);
                    services.AddGoogleAuthentication(configureOptions);
                    services.AddFacebookAuthentication(o =>
                    {
                        o.AppId = "Test AppId";
                        o.AppSecret = "Test AppSecrent";
                    });
                });
            return new TestServer(builder);
        }
    }
}
