// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.Security.DataHandler;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.AspNet.Security.OpenIdConnect;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Security.Tests.OpenIdConnect
{
    public class OpenIdConnectMiddlewareTests
    {
        static string noncePrefix = "OpenIdConnect." + "Nonce.";
        static string nonceDelimiter = ".";

        [Fact]
        public async Task ChallengeWillTriggerRedirect()
        {
            var server = CreateServer(options =>
            {
                options.Authority = "https://login.windows.net/common";
                options.ClientId = "Test Id";
                options.SignInAsAuthenticationType = OpenIdConnectAuthenticationDefaults.AuthenticationType;
            });
            var transaction = await SendAsync(server, "https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.ToString();
            location.ShouldContain("https://login.windows.net/common/oauth2/authorize?");
            location.ShouldContain("client_id=");
            location.ShouldContain("&response_type=");
            location.ShouldContain("&scope=");
            location.ShouldContain("&state=");
            location.ShouldContain("&response_mode=");
        }

        [Fact]
        public async Task ChallengeWillSetNonceCookie()
        {
            var server = CreateServer(options =>
            {
                options.Authority = "https://login.windows.net/common";
                options.ClientId = "Test Id";
            });
            var transaction = await SendAsync(server, "https://example.com/challenge");
            transaction.SetCookie.Single().ShouldContain("OpenIdConnect.nonce.");
        }

        [Fact]
        public async Task ChallengeWillSetDefaultScope()
        {
            var server = CreateServer(options =>
            {
                options.Authority = "https://login.windows.net/common";
                options.ClientId = "Test Id";
            });
            var transaction = await SendAsync(server, "https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.Query.ShouldContain("&scope=" + Uri.EscapeDataString("openid profile"));
        }

        [Fact]
        public async Task ChallengeWillUseOptionsProperties()
        {
            var server = CreateServer(options =>
            {
                options.Authority = "https://login.windows.net/common";
                options.ClientId = "Test Id";
                options.SignInAsAuthenticationType = OpenIdConnectAuthenticationDefaults.AuthenticationType;
                options.Scope = "https://www.googleapis.com/auth/plus.login";
                options.ResponseType = "id_token";
            });
            var transaction = await SendAsync(server, "https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("scope=" + Uri.EscapeDataString("https://www.googleapis.com/auth/plus.login"));
            query.ShouldContain("response_type=" + Uri.EscapeDataString("id_token"));
        }

        [Fact]
        public async Task ChallengeWillUseNotifications()
        {
            ISecureDataFormat<AuthenticationProperties> stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("GoogleTest"));
            var server = CreateServer(options =>
            {
                options.Authority = "https://login.windows.net/common";
                options.ClientId = "Test Id";
                options.Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    MessageReceived = notification =>
                        {
                            notification.ProtocolMessage.Scope = "test openid profile";
                            notification.HandleResponse();
                            return Task.FromResult<object>(null);
                        }
                };
            });

            var properties = new AuthenticationProperties();
            var state = stateFormat.Protect(properties);
            var transaction = await SendAsync(server,"https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }


        [Fact]
        public async Task SignOutWithDefaultRedirectUri()
        {
            ISecureDataFormat<AuthenticationProperties> stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("GoogleTest"));
            var server = CreateServer(options =>
            {
                options.Authority = "https://login.windows.net/common";
                options.ClientId = "Test Id";
            });

            var transaction = await SendAsync(server, "https://example.com/signout");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.AbsoluteUri.ShouldBe("https://login.windows.net/common/oauth2/logout");
        }

        [Fact]
        public async Task SignOutWithCustomRedirectUri()
        {
            ISecureDataFormat<AuthenticationProperties> stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("GoogleTest"));
            var server = CreateServer(options =>
            {
                options.Authority = "https://login.windows.net/common";
                options.ClientId = "Test Id";
                options.PostLogoutRedirectUri = "https://example.com/logout";
            });

            var transaction = await SendAsync(server, "https://example.com/signout");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            transaction.Response.Headers.Location.AbsoluteUri.ShouldContain(Uri.EscapeDataString("https://example.com/logout"));
        }

        [Fact]
        // Test Cases for calculating the expiration time of cookie from cookie name
        public void NonceCookieExpirationTime()
        {
            DateTime utcNow = DateTime.UtcNow;

            GetNonceExpirationTime(noncePrefix + DateTime.MaxValue.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)).ShouldBe(DateTime.MaxValue);

            GetNonceExpirationTime(noncePrefix + DateTime.MinValue.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)).ShouldBe(DateTime.MinValue + TimeSpan.FromHours(1));

            GetNonceExpirationTime(noncePrefix + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)).ShouldBe(utcNow + TimeSpan.FromHours(1));

            GetNonceExpirationTime(noncePrefix, TimeSpan.FromHours(1)).ShouldBe(DateTime.MinValue);

            GetNonceExpirationTime("", TimeSpan.FromHours(1)).ShouldBe(DateTime.MinValue);

            GetNonceExpirationTime(noncePrefix + noncePrefix, TimeSpan.FromHours(1)).ShouldBe(DateTime.MinValue);

            GetNonceExpirationTime(noncePrefix + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)).ShouldBe(utcNow + TimeSpan.FromHours(1));

            GetNonceExpirationTime(utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter + utcNow.Ticks.ToString(CultureInfo.InvariantCulture) + nonceDelimiter, TimeSpan.FromHours(1)).ShouldBe(DateTime.MinValue);
        }

        private static TestServer CreateServer(Action<OpenIdConnectAuthenticationOptions> configureOptions, Func<HttpContext, Task> handler = null)
        {
            return TestServer.Create(app =>
            {
                app.UseServices(services =>
                {
                    services.AddDataProtection();
                    services.Configure<ExternalAuthenticationOptions>(options =>
                    {
                        options.SignInAsAuthenticationType = CookieAuthenticationDefaults.AuthenticationType;
                    });
                });

                app.UseCookieAuthentication(options =>
                {
                    options.AuthenticationType = "OpenIdConnect";
                });
                app.UseOpenIdConnectAuthentication(configureOptions);
                app.Use(async (context, next) =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    if (req.Path == new PathString("/challenge"))
                    {
                        res.Challenge("OpenIdConnect");
                        res.StatusCode = 401;
                    }
                    else if (req.Path == new PathString("/signin"))
                    {
                        res.SignIn();
                    }
                    else if (req.Path == new PathString("/signout"))
                    {
                        res.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType);
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

            public string FindClaimValue(string claimType)
            {
                XElement claim = ResponseElement.Elements("claim").SingleOrDefault(elt => elt.Attribute("type").Value == claimType);
                if (claim == null)
                {
                    return null;
                }
                return claim.Attribute("value").Value;
            }
        }
        private static void Describe(HttpResponse res, ClaimsIdentity identity)
        {
            res.StatusCode = 200;
            res.ContentType = "text/xml";
            var xml = new XElement("xml");
            if (identity != null)
            {
                xml.Add(identity.Claims.Select(claim => new XElement("claim", new XAttribute("type", claim.Type), new XAttribute("value", claim.Value))));
            }
            using (var memory = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(memory, Encoding.UTF8))
                {
                    xml.WriteTo(writer);
                }
                res.Body.Write(memory.ToArray(), 0, memory.ToArray().Length);
            }
        }

        private class TestHttpMessageHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, HttpResponseMessage> Sender { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                if (Sender != null)
                {
                    return Task.FromResult(Sender(request));
                }

                return Task.FromResult<HttpResponseMessage>(null);
            }
        }

        private static HttpResponseMessage ReturnJsonResponse(object content)
        {
            var res = new HttpResponseMessage(HttpStatusCode.OK);
            var text = JsonConvert.SerializeObject(content);
            res.Content = new StringContent(text, Encoding.UTF8, "application/json");
            return res;
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