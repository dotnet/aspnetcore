// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    public class OpenIdConnectMiddlewareTests
    {
        static string noncePrefix = "OpenIdConnect." + "Nonce.";
        static string nonceDelimiter = ".";
        const string Challenge = "/challenge";
        const string ChallengeWithOutContext = "/challengeWithOutContext";
        const string ChallengeWithProperties = "/challengeWithProperties";
        const string DefaultHost = @"https://example.com";
        const string DefaultAuthority = @"https://example.com/common";
        const string ExpectedAuthorizeRequest = @"https://example.com/common/oauth2/signin";
        const string ExpectedLogoutRequest = @"https://example.com/common/oauth2/logout";
        const string Logout = "/logout";
        const string Signin = "/signin";
        const string Signout = "/signout";

        [Fact]
        public async Task ChallengeWillIssueHtmlFormWhenEnabled()
        {
            var server = CreateServer(options =>
            {
                options.Authority = DefaultAuthority;
                options.ClientId = "Test Id";
                options.Configuration = TestUtilities.DefaultOpenIdConnectConfiguration;
                options.AuthenticationMethod = OpenIdConnectRedirectBehavior.FormPost;
            });
            var transaction = await SendAsync(server, DefaultHost + Challenge);
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.Equal("text/html", transaction.Response.Content.Headers.ContentType.MediaType);
            Assert.Contains("form", transaction.ResponseText);
        }

        [Fact]
        public async Task ChallengeWillSetDefaults()
        {
            var stateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
            var queryValues = ExpectedQueryValues.Defaults(DefaultAuthority);
            queryValues.State = OpenIdConnectDefaults.AuthenticationPropertiesKey + "=" + stateDataFormat.Protect(new AuthenticationProperties());
            var server = CreateServer(options =>
            {
                SetOptions(options, DefaultParameters(), queryValues);
            });

            var transaction = await SendAsync(server, DefaultHost + Challenge);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            queryValues.CheckValues(transaction.Response.Headers.Location.AbsoluteUri, DefaultParameters());
        }

        [Fact]
        public async Task ChallengeWillSetNonceAndStateCookies()
        {
            var server = CreateServer(options =>
            {
                options.Authority = DefaultAuthority;
                options.ClientId = "Test Id";
                options.Configuration = TestUtilities.DefaultOpenIdConnectConfiguration;
            });
            var transaction = await SendAsync(server, DefaultHost + Challenge);

            var firstCookie = transaction.SetCookie.First();
            Assert.Contains(OpenIdConnectDefaults.CookieNoncePrefix, firstCookie);
            Assert.Contains("expires", firstCookie);

            var secondCookie = transaction.SetCookie.Skip(1).First();
            Assert.Contains(OpenIdConnectDefaults.CookieStatePrefix, secondCookie);
            Assert.Contains("expires", secondCookie);
        }

        [Fact]
        public async Task ChallengeWillUseOptionsProperties()
        {
            var queryValues = new ExpectedQueryValues(DefaultAuthority);
            var server = CreateServer(options =>
            {
                SetOptions(options, DefaultParameters(), queryValues);
            });

            var transaction = await SendAsync(server, DefaultHost + Challenge);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            queryValues.CheckValues(transaction.Response.Headers.Location.AbsoluteUri, DefaultParameters());
        }

        /// <summary>
        /// Tests RedirectForAuthenticationContext replaces the OpenIdConnectMesssage correctly.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task ChallengeSettingMessage()
        {
            var configuration = new OpenIdConnectConfiguration
            {
                AuthorizationEndpoint = ExpectedAuthorizeRequest,
            };

            var queryValues = new ExpectedQueryValues(DefaultAuthority, configuration)
            {
                RequestType = OpenIdConnectRequestType.AuthenticationRequest
            };
            var server = CreateServer(SetProtocolMessageOptions);
            var transaction = await SendAsync(server, DefaultHost + Challenge);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            queryValues.CheckValues(transaction.Response.Headers.Location.AbsoluteUri, new string[] {});
        }

        /// <summary>
        /// Tests RedirectForSignOutContext replaces the OpenIdConnectMesssage correctly.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task SignOutSettingMessage()
        {
            var configuration = new OpenIdConnectConfiguration
            {
                EndSessionEndpoint = ExpectedLogoutRequest
            };

            var queryValues = new ExpectedQueryValues(DefaultAuthority, configuration)
            {
                RequestType = OpenIdConnectRequestType.LogoutRequest
            };
            var server = CreateServer(SetProtocolMessageOptions);
            var transaction = await SendAsync(server, DefaultHost + Signout);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            queryValues.CheckValues(transaction.Response.Headers.Location.AbsoluteUri, new string[] { });
        }

        private static void SetProtocolMessageOptions(OpenIdConnectOptions options)
        {
            var fakeOpenIdRequestMessage = new FakeOpenIdConnectMessage(ExpectedAuthorizeRequest, ExpectedLogoutRequest);
            options.AutomaticChallenge = true;
            options.Events = new OpenIdConnectEvents()
            {
                OnRedirectToAuthenticationEndpoint = (context) =>
                {
                    context.ProtocolMessage = fakeOpenIdRequestMessage;
                    return Task.FromResult(0);
                },
                OnRedirectToEndSessionEndpoint = (context) =>
                {
                    context.ProtocolMessage = fakeOpenIdRequestMessage;
                    return Task.FromResult(0);
                }
            };
        }
        private class FakeOpenIdConnectMessage : OpenIdConnectMessage
        {
            private readonly string _authorizeRequest;
            private readonly string _logoutRequest;

            public FakeOpenIdConnectMessage(string authorizeRequest, string logoutRequest)
            {
                _authorizeRequest = authorizeRequest;
                _logoutRequest = logoutRequest;
            }

            public override string CreateAuthenticationRequestUrl()
            {
                return _authorizeRequest;
            }

            public override string CreateLogoutRequestUrl()
            {
                return _logoutRequest;
            }
        }

        /// <summary>
        /// Tests for users who want to add 'state'. There are two ways to do it.
        /// 1. Users set 'state' (OpenIdConnectMessage.State) in the event. The runtime appends to that state.
        /// 2. Users add to the AuthenticationProperties (context.AuthenticationProperties), values will be serialized.
        /// </summary>
        /// <param name="userSetsState"></param>
        /// <returns></returns>
        [Theory, MemberData("StateDataSet")]
        public async Task ChallengeSettingState(string userState, string challenge)
        {
            var queryValues = new ExpectedQueryValues(DefaultAuthority);
            var stateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
            var properties = new AuthenticationProperties();
            if (challenge == ChallengeWithProperties)
            {
                properties.Items.Add("item1", Guid.NewGuid().ToString());
            }

            var server = CreateServer(options =>
            {
                SetOptions(options, DefaultParameters(new string[] { OpenIdConnectParameterNames.State }), queryValues, stateDataFormat);
                options.AutomaticChallenge = challenge.Equals(ChallengeWithOutContext);
                options.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToAuthenticationEndpoint = context =>
                    {
                        context.ProtocolMessage.State = userState;
                        return Task.FromResult<object>(null);
                    }

                };
            }, null, properties);

            var transaction = await SendAsync(server, DefaultHost + challenge);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

            if (challenge != ChallengeWithProperties)
            {
                if (userState != null)
                {
                    properties.Items.Add(OpenIdConnectDefaults.UserstatePropertiesKey, userState);
                }
                properties.Items.Add(OpenIdConnectDefaults.RedirectUriForCodePropertiesKey, queryValues.RedirectUri);
            }

            queryValues.State = stateDataFormat.Protect(properties);
            queryValues.CheckValues(transaction.Response.Headers.Location.AbsoluteUri, DefaultParameters(new string[] { OpenIdConnectParameterNames.State }));
        }

        public static TheoryData<string, string> StateDataSet
        {
            get
            {
                var dataset = new TheoryData<string, string>();
                dataset.Add(Guid.NewGuid().ToString(), Challenge);
                dataset.Add(null, Challenge);
                dataset.Add(Guid.NewGuid().ToString(), ChallengeWithOutContext);
                dataset.Add(null, ChallengeWithOutContext);
                dataset.Add(Guid.NewGuid().ToString(), ChallengeWithProperties);
                dataset.Add(null, ChallengeWithProperties);

                return dataset;
            }
        }

        [Fact]
        public async Task ChallengeWillUseEvents()
        {
            var queryValues = new ExpectedQueryValues(DefaultAuthority);
            var queryValuesSetInEvent = new ExpectedQueryValues(DefaultAuthority);
            var server = CreateServer(options =>
            {
                SetOptions(options, DefaultParameters(), queryValues);
                options.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToAuthenticationEndpoint = context =>
                    {
                        context.ProtocolMessage.ClientId = queryValuesSetInEvent.ClientId;
                        context.ProtocolMessage.RedirectUri = queryValuesSetInEvent.RedirectUri;
                        context.ProtocolMessage.Resource = queryValuesSetInEvent.Resource;
                        context.ProtocolMessage.Scope = queryValuesSetInEvent.Scope;
                        return Task.FromResult<object>(null);
                    }
                };
            });

            var transaction = await SendAsync(server, DefaultHost + Challenge);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            queryValuesSetInEvent.CheckValues(transaction.Response.Headers.Location.AbsoluteUri, DefaultParameters());
        }

        private void SetOptions(OpenIdConnectOptions options, List<string> parameters, ExpectedQueryValues queryValues, ISecureDataFormat<AuthenticationProperties> secureDataFormat = null)
        {
            foreach (var param in parameters)
            {
                if (param.Equals(OpenIdConnectParameterNames.ClientId))
                    options.ClientId = queryValues.ClientId;
                else if (param.Equals(OpenIdConnectParameterNames.RedirectUri))
                    options.RedirectUri = queryValues.RedirectUri;
                else if (param.Equals(OpenIdConnectParameterNames.Resource))
                    options.Resource = queryValues.Resource;
                else if (param.Equals(OpenIdConnectParameterNames.Scope)) {
                    options.Scope.Clear();

                    foreach (var scope in queryValues.Scope.Split(' ')) {
                        options.Scope.Add(scope);
                    }
                }
            }

            options.Authority = queryValues.Authority;
            options.Configuration = queryValues.Configuration;
            options.StateDataFormat = secureDataFormat ?? new AuthenticationPropertiesFormaterKeyValue();
        }

        private List<string> DefaultParameters(string[] additionalParams = null)
        {
            var parameters =
                new List<string>
                {
                    OpenIdConnectParameterNames.ClientId,
                    OpenIdConnectParameterNames.RedirectUri,
                    OpenIdConnectParameterNames.Resource,
                    OpenIdConnectParameterNames.ResponseMode,
                    OpenIdConnectParameterNames.Scope,
                };

            if (additionalParams != null)
                parameters.AddRange(additionalParams);

            return parameters;
        }

        private static void DefaultChallengeOptions(OpenIdConnectOptions options)
        {
            options.AuthenticationScheme = "OpenIdConnectHandlerTest";
            options.AutomaticChallenge = true;
            options.ClientId = Guid.NewGuid().ToString();
            options.ConfigurationManager = TestUtilities.DefaultOpenIdConnectConfigurationManager;
            options.StateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
        }

        [Fact]
        public async Task SignOutWithDefaultRedirectUri()
        {
            var configuration = TestUtilities.DefaultOpenIdConnectConfiguration;
            var server = CreateServer(options =>
            {
                options.Authority = DefaultAuthority;
                options.ClientId = "Test Id";
                options.Configuration = configuration;
            });

            var transaction = await SendAsync(server, DefaultHost + Signout);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal(configuration.EndSessionEndpoint, transaction.Response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SignOutWithCustomRedirectUri()
        {
            var configuration = TestUtilities.DefaultOpenIdConnectConfiguration;
            var server = CreateServer(options =>
            {
                options.Authority = DefaultAuthority;
                options.ClientId = "Test Id";
                options.Configuration = configuration;
                options.PostLogoutRedirectUri = "https://example.com/logout";
            });

            var transaction = await SendAsync(server, DefaultHost + Signout);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Contains(UrlEncoder.Default.UrlEncode("https://example.com/logout"), transaction.Response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SignOutWith_Specific_RedirectUri_From_Authentication_Properites()
        {
            var configuration = TestUtilities.DefaultOpenIdConnectConfiguration;
            var server = CreateServer(options =>
            {
                options.Authority = DefaultAuthority;
                options.ClientId = "Test Id";
                options.Configuration = configuration;
                options.PostLogoutRedirectUri = "https://example.com/logout";
            });

            var transaction = await SendAsync(server, "https://example.com/signout_with_specific_redirect_uri");
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Contains(UrlEncoder.Default.UrlEncode("http://www.example.com/specific_redirect_uri"), transaction.Response.Headers.Location.AbsoluteUri);
        }

        private static TestServer CreateServer(Action<OpenIdConnectOptions> configureOptions, Func<HttpContext, Task> handler = null, AuthenticationProperties properties = null)
        {
            return TestServer.Create(app =>
            {
                app.UseCookieAuthentication(options =>
                {
                    options.AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                });
                app.UseOpenIdConnectAuthentication(configureOptions);
                app.Use(async (context, next) =>
                {
                    var req = context.Request;
                    var res = context.Response;

                    if (req.Path == new PathString(Challenge))
                    {
                        await context.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
                    }
                    else if (req.Path == new PathString(ChallengeWithProperties))
                    {
                        await context.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
                    }
                    else if (req.Path == new PathString(ChallengeWithOutContext))
                    {
                        res.StatusCode = 401;
                    }
                    else if (req.Path == new PathString(Signin))
                    {
                        // REVIEW: this used to just be res.SignIn()
                        await context.Authentication.SignInAsync(OpenIdConnectDefaults.AuthenticationScheme, new ClaimsPrincipal());
                    }
                    else if (req.Path == new PathString(Signout))
                    {
                        await context.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                    }
                    else if (req.Path == new PathString("/signout_with_specific_redirect_uri"))
                    {
                        await context.Authentication.SignOutAsync(
                            OpenIdConnectDefaults.AuthenticationScheme,
                            new AuthenticationProperties() { RedirectUri = "http://www.example.com/specific_redirect_uri" });
                    }
                    else if (handler != null)
                    {
                        await handler(context);
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
                services.Configure<SharedAuthenticationOptions>(options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                });
            });
        }

        private static async Task<Transaction> SendAsync(TestServer server, string uri, string cookieHeader = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }

            var transaction = new Transaction
            {
                Request = request,
                Response = await server.CreateClient().SendAsync(request),
            };

            if (transaction.Response.Headers.Contains("Set-Cookie"))
            {
                transaction.SetCookie = transaction.Response.Headers.GetValues("Set-Cookie").ToList();
            }

            transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();
            if (transaction.Response.Content != null &&
                transaction.Response.Content.Headers.ContentType != null &&
                transaction.Response.Content.Headers.ContentType.MediaType == "text/xml")
            {
                transaction.ResponseElement = XElement.Parse(transaction.ResponseText);
            }

            return transaction;
        }

        private class Transaction
        {
            public HttpRequestMessage Request { get; set; }

            public HttpResponseMessage Response { get; set; }

            public IList<string> SetCookie { get; set; }

            public string ResponseText { get; set; }

            public XElement ResponseElement { get; set; }

            public string AuthenticationCookieValue
            {
                get
                {
                    if (SetCookie != null && SetCookie.Count > 0)
                    {
                        var authCookie = SetCookie.SingleOrDefault(c => c.Contains(".AspNet.Cookie="));
                        if (authCookie != null)
                        {
                            return authCookie.Substring(0, authCookie.IndexOf(';'));
                        }
                    }

                    return null;
                }
            }
        }

        [Fact]
        // Test Cases for calculating the expiration time of cookie from cookie name
        public void NonceCookieExpirationTime()
        {
            DateTime utcNow = DateTime.UtcNow;

            Assert.Equal(DateTime.MaxValue, GetNonceExpirationTime(noncePrefix + DateTime.MaxValue.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));

            Assert.Equal(DateTime.MinValue + TimeSpan.FromHours(1), GetNonceExpirationTime(noncePrefix + DateTime.MinValue.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));

            Assert.Equal(utcNow + TimeSpan.FromHours(1), GetNonceExpirationTime(noncePrefix + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));

            Assert.Equal(DateTime.MinValue, GetNonceExpirationTime(noncePrefix, TimeSpan.FromHours(1)));

            Assert.Equal(DateTime.MinValue, GetNonceExpirationTime("", TimeSpan.FromHours(1)));

            Assert.Equal(DateTime.MinValue, GetNonceExpirationTime(noncePrefix + noncePrefix, TimeSpan.FromHours(1)));

            Assert.Equal(utcNow + TimeSpan.FromHours(1), GetNonceExpirationTime(noncePrefix + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));

            Assert.Equal(DateTime.MinValue, GetNonceExpirationTime(utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)));
        }

        private static DateTime GetNonceExpirationTime(string keyname, TimeSpan nonceLifetime)
        {
            DateTime nonceTime = DateTime.MinValue;
            string timestamp = null;
            int endOfTimestamp;
            if (keyname.StartsWith(noncePrefix, StringComparison.Ordinal))
            {
                timestamp = keyname.Substring(noncePrefix.Length);
                endOfTimestamp = timestamp.IndexOf('.');

                if (endOfTimestamp != -1)
                {
                    timestamp = timestamp.Substring(0, endOfTimestamp);
                    try
                    {
                        nonceTime = DateTime.FromBinary(Convert.ToInt64(timestamp, CultureInfo.InvariantCulture));
                        if ((nonceTime >= DateTime.UtcNow) && ((DateTime.MaxValue - nonceTime) < nonceLifetime))
                            nonceTime = DateTime.MaxValue;
                        else
                            nonceTime += nonceLifetime;
                    }
                    catch
                    {
                    }
                }
            }

            return nonceTime;
        }
    }
}