// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.DataHandler;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.WebEncoders;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication.Google
{
    public class GoogleMiddlewareTests
    {
        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.ToString();
            location.ShouldContain("https://accounts.google.com/o/oauth2/auth?response_type=code");
            location.ShouldContain("&client_id=");
            location.ShouldContain("&redirect_uri=");
            location.ShouldContain("&scope=");
            location.ShouldContain("&state=");

            location.ShouldNotContain("access_type=");
            location.ShouldNotContain("approval_prompt=");
            location.ShouldNotContain("login_hint=");
        }

        [Fact]
        public async Task Challenge401WillTriggerRedirection()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.AutomaticAuthentication = true;
            });
            var transaction = await server.SendAsync("https://example.com/401");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.ToString();
            location.ShouldContain("https://accounts.google.com/o/oauth2/auth?response_type=code");
            location.ShouldContain("&client_id=");
            location.ShouldContain("&redirect_uri=");
            location.ShouldContain("&scope=");
            location.ShouldContain("&state=");
        }

        [Fact]
        public async Task ChallengeWillSetCorrelationCookie()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            Console.WriteLine(transaction.SetCookie);
            transaction.SetCookie.Single().ShouldContain(".AspNet.Correlation.Google=");
        }

        [Fact]
        public async Task Challenge401WillSetCorrelationCookie()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.AutomaticAuthentication = true;
            });
            var transaction = await server.SendAsync("https://example.com/401");
            Console.WriteLine(transaction.SetCookie);
            transaction.SetCookie.Single().ShouldContain(".AspNet.Correlation.Google=");
        }

        [Fact]
        public async Task ChallengeWillSetDefaultScope()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("&scope=" + UrlEncoder.Default.UrlEncode("openid profile email"));
        }

        [Fact]
        public async Task Challenge401WillSetDefaultScope()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.AutomaticAuthentication = true;
            });
            var transaction = await server.SendAsync("https://example.com/401");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("&scope=" + UrlEncoder.Default.UrlEncode("openid profile email"));
        }

        [Fact]
        public async Task ChallengeWillUseAuthenticationPropertiesAsParameters()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.AutomaticAuthentication = true;
            },
            context =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    if (req.Path == new PathString("/challenge2"))
                    {
                        context.Authentication.Challenge("Google", new AuthenticationProperties(
                            new Dictionary<string, string>()
                            {
                                { "scope", "https://www.googleapis.com/auth/plus.login" },
                                { "access_type", "offline" },
                                { "approval_prompt", "force" },
                                { "login_hint", "test@example.com" }
                            }));
                        res.StatusCode = 401;
                    }

                    return Task.FromResult<object>(null);
                });
            var transaction = await server.SendAsync("https://example.com/challenge2");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("scope=" + UrlEncoder.Default.UrlEncode("https://www.googleapis.com/auth/plus.login"));
            query.ShouldContain("access_type=offline");
            query.ShouldContain("approval_prompt=force");
            query.ShouldContain("login_hint=" + UrlEncoder.Default.UrlEncode("test@example.com"));
        }

        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.Notifications = new GoogleAuthenticationNotifications
                {
                    OnApplyRedirect = context =>
                        {
                            context.Response.Redirect(context.RedirectUri + "&custom=test");
                        }
                };
            });
            var transaction = await server.SendAsync("https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("custom=test");
        }

        // TODO: Fix these tests to path (Need some test logic for Authenticate("Google") to return a ticket still
        //[Fact]
        //public async Task GoogleTurns401To403WhenAuthenticated()
        //{
        //    TestServer server = CreateServer(options =>
        //    {
        //        options.ClientId = "Test Id";
        //        options.ClientSecret = "Test Secret";
        //    });

        //    Transaction transaction1 = await SendAsync(server, "http://example.com/unauthorized");
        //    transaction1.Response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        //}

        //[Fact]
        //public async Task GoogleTurns401To403WhenAutomatic()
        //{
        //    TestServer server = CreateServer(options =>
        //    {
        //        options.ClientId = "Test Id";
        //        options.ClientSecret = "Test Secret";
        //        options.AutomaticAuthentication = true;
        //    });

        //    Debugger.Launch();
        //    Transaction transaction1 = await SendAsync(server, "http://example.com/unauthorizedAuto");
        //    transaction1.Response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        //}

        [Fact]
        public async Task ReplyPathWithoutStateQueryStringWillBeRejected()
        {
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
            });
            var transaction = await server.SendAsync("https://example.com/signin-google?code=TestCode");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        }



        [Theory]
        [InlineData(null)]
        [InlineData("CustomIssuer")]
        public async Task ReplyPathWillAuthenticateValidAuthorizeCodeAndState(string claimsIssuer)
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("GoogleTest"));
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.StateDataFormat = stateFormat;
                options.ClaimsIssuer = claimsIssuer;
                options.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri == "https://accounts.google.com/o/oauth2/token")
                        {
                            return ReturnJsonResponse(new
                            {
                                access_token = "Test Access Token",
                                expire_in = 3600,
                                token_type = "Bearer"
                            });
                        }
                        else if (req.RequestUri.GetLeftPart(UriPartial.Path) == "https://www.googleapis.com/plus/v1/people/me")
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

                        return null;
                    }
                };
            });
            var properties = new AuthenticationProperties();
            var correlationKey = ".AspNet.Correlation.Google";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.UrlEncode(state),
                correlationKey + "=" + correlationValue);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.ToString().ShouldBe("/me");
            transaction.SetCookie.Count.ShouldBe(2);
            transaction.SetCookie[0].ShouldContain(correlationKey);
            transaction.SetCookie[1].ShouldContain(".AspNet." + TestExtensions.CookieAuthenticationScheme);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await server.SendAsync("https://example.com/me", authCookie);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var expectedIssuer = claimsIssuer ?? GoogleAuthenticationDefaults.AuthenticationScheme;
            transaction.FindClaimValue(ClaimTypes.Name, expectedIssuer).ShouldBe("Test Name");
            transaction.FindClaimValue(ClaimTypes.NameIdentifier, expectedIssuer).ShouldBe("Test User ID");
            transaction.FindClaimValue(ClaimTypes.GivenName, expectedIssuer).ShouldBe("Test Given Name");
            transaction.FindClaimValue(ClaimTypes.Surname, expectedIssuer).ShouldBe("Test Family Name");
            transaction.FindClaimValue(ClaimTypes.Email, expectedIssuer).ShouldBe("Test email");

            // Ensure claims transformation 
            transaction.FindClaimValue("xform").ShouldBe("yup");
        }

        [Fact]
        public async Task ReplyPathWillRejectIfCodeIsInvalid()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("GoogleTest"));
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.StateDataFormat = stateFormat;
                options.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest);
                    }
                };
            });
            var properties = new AuthenticationProperties();
            var correlationKey = ".AspNet.Correlation.Google";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.UrlEncode(state),
                correlationKey + "=" + correlationValue);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.ToString().ShouldContain("error=access_denied");
        }

        [Fact]
        public async Task ReplyPathWillRejectIfAccessTokenIsMissing()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("GoogleTest"));
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.StateDataFormat = stateFormat;
                options.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        return ReturnJsonResponse(new object());
                    }
                };
            });
            var properties = new AuthenticationProperties();
            var correlationKey = ".AspNet.Correlation.Google";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.UrlEncode(state),
                correlationKey + "=" + correlationValue);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.ToString().ShouldContain("error=access_denied");
        }

        [Fact]
        public async Task AuthenticatedEventCanGetRefreshToken()
        {
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("GoogleTest"));
            var server = CreateServer(options =>
            {
                options.ClientId = "Test Id";
                options.ClientSecret = "Test Secret";
                options.StateDataFormat = stateFormat;
                options.BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri == "https://accounts.google.com/o/oauth2/token")
                        {
                            return ReturnJsonResponse(new
                            {
                                access_token = "Test Access Token",
                                expire_in = 3600,
                                token_type = "Bearer",
                                refresh_token = "Test Refresh Token"
                            });
                        }
                        else if (req.RequestUri.GetLeftPart(UriPartial.Path) == "https://www.googleapis.com/plus/v1/people/me")
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

                        return null;
                    }
                };
                options.Notifications = new GoogleAuthenticationNotifications()
                {
                    OnAuthenticated = context =>
                        {
                            var refreshToken = context.RefreshToken;
                            context.Principal.AddIdentity(new ClaimsIdentity(new Claim[] { new Claim("RefreshToken", refreshToken, ClaimValueTypes.String, "Google") }, "Google"));
                            return Task.FromResult<object>(null);
                        }
                };
            });
            var properties = new AuthenticationProperties();
            var correlationKey = ".AspNet.Correlation.Google";
            var correlationValue = "TestCorrelationId";
            properties.Items.Add(correlationKey, correlationValue);
            properties.RedirectUri = "/me";
            var state = stateFormat.Protect(properties);
            var transaction = await server.SendAsync(
                "https://example.com/signin-google?code=TestCode&state=" + UrlEncoder.Default.UrlEncode(state),
                correlationKey + "=" + correlationValue);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.ToString().ShouldBe("/me");
            transaction.SetCookie.Count.ShouldBe(2);
            transaction.SetCookie[0].ShouldContain(correlationKey);
            transaction.SetCookie[1].ShouldContain(".AspNet." + TestExtensions.CookieAuthenticationScheme);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await server.SendAsync("https://example.com/me", authCookie);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            transaction.FindClaimValue("RefreshToken").ShouldBe("Test Refresh Token");
        }

        private static HttpResponseMessage ReturnJsonResponse(object content)
        {
            var res = new HttpResponseMessage(HttpStatusCode.OK);
            var text = JsonConvert.SerializeObject(content);
            res.Content = new StringContent(text, Encoding.UTF8, "application/json");
            return res;
        }

        private static TestServer CreateServer(Action<GoogleAuthenticationOptions> configureOptions, Func<HttpContext, Task> testpath = null)
        {
            return TestServer.Create(app =>
            {
                app.UseCookieAuthentication(options =>
                {
                    options.AuthenticationScheme = TestExtensions.CookieAuthenticationScheme;
                    options.AutomaticAuthentication = true;
                });
                app.UseGoogleAuthentication(configureOptions);
                app.UseClaimsTransformation();
                app.Use(async (context, next) =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    if (req.Path == new PathString("/challenge"))
                    {
                        context.Authentication.Challenge("Google");
                        res.StatusCode = 401;
                    }
                    else if (req.Path == new PathString("/me"))
                    {
                        res.Describe(context.User);
                    }
                    else if (req.Path == new PathString("/unauthorized"))
                    {
                        // Simulate Authorization failure 
                        var result = await context.Authentication.AuthenticateAsync("Google");
                        context.Authentication.Challenge("Google");
                    }
                    else if (req.Path == new PathString("/unauthorizedAuto"))
                    {
                        var result = await context.Authentication.AuthenticateAsync("Google");
                        res.StatusCode = 401;
                        context.Authentication.Challenge();
                    }
                    else if (req.Path == new PathString("/401"))
                    {
                        res.StatusCode = 401;
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
            },
            services =>
            {
                services.AddAuthentication();
                services.Configure<ExternalAuthenticationOptions>(options =>
                {
                    options.SignInScheme = TestExtensions.CookieAuthenticationScheme;
                });
                services.ConfigureClaimsTransformation(p =>
                {
                    var id = new ClaimsIdentity("xform");
                    id.AddClaim(new Claim("xform", "yup"));
                    p.AddIdentity(id);
                    return p;
                });
            });
        }

    }
}
