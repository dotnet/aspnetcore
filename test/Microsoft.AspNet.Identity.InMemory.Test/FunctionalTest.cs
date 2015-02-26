// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Shouldly;
using Xunit;
using Microsoft.AspNet.Identity.Test;

namespace Microsoft.AspNet.Identity.InMemory
{
    public class FunctionalTest
    {
        const string TestPassword = "1qaz!QAZ";

        [Fact]
        public async Task CanCreateMeLoginAndCookieStopsWorkingAfterExpiration()
        {
            var clock = new TestClock();
            var server = CreateServer(appCookieOptions =>
            {
                appCookieOptions.SystemClock = clock;
                appCookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                appCookieOptions.SlidingExpiration = false;
            });

            var transaction1 = await SendAsync(server, "http://example.com/createMe");
            transaction1.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Assert.Null(transaction1.SetCookie);

            var transaction2 = await SendAsync(server, "http://example.com/pwdLogin/false");
            transaction2.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Assert.NotNull(transaction2.SetCookie);
            transaction2.SetCookie.ShouldNotContain("; expires=");

            var transaction3 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
            FindClaimValue(transaction3, ClaimTypes.Name).ShouldBe("hao");
            Assert.Null(transaction3.SetCookie);

            clock.Add(TimeSpan.FromMinutes(7));

            var transaction4 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
            FindClaimValue(transaction4, ClaimTypes.Name).ShouldBe("hao");
            Assert.Null(transaction4.SetCookie);

            clock.Add(TimeSpan.FromMinutes(7));

            var transaction5 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
            FindClaimValue(transaction5, ClaimTypes.Name).ShouldBe(null);
            Assert.Null(transaction5.SetCookie);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanCreateMeLoginAndSecurityStampExtendsExpiration(bool rememberMe)
        {
            var clock = new TestClock();
            var server = CreateServer(appCookieOptions =>
            {
                appCookieOptions.SystemClock = clock;
            });

            var transaction1 = await SendAsync(server, "http://example.com/createMe");
            transaction1.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Assert.Null(transaction1.SetCookie);

            var transaction2 = await SendAsync(server, "http://example.com/pwdLogin/" + rememberMe);
            transaction2.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Assert.NotNull(transaction2.SetCookie);
            if (rememberMe)
            {
                transaction2.SetCookie.ShouldContain("; expires=");
            }
            else
            {
                transaction2.SetCookie.ShouldNotContain("; expires=");
            }

            var transaction3 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
            FindClaimValue(transaction3, ClaimTypes.Name).ShouldBe("hao");
            Assert.Null(transaction3.SetCookie);

            // Make sure we don't get a new cookie yet
            clock.Add(TimeSpan.FromMinutes(10));
            var transaction4 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
            FindClaimValue(transaction4, ClaimTypes.Name).ShouldBe("hao");
            Assert.Null(transaction4.SetCookie);

            // Go past SecurityStampValidation interval and ensure we get a new cookie
            clock.Add(TimeSpan.FromMinutes(21));

            var transaction5 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
            Assert.NotNull(transaction5.SetCookie);
            FindClaimValue(transaction5, ClaimTypes.Name).ShouldBe("hao");

            // Make sure new cookie is valid
            var transaction6 = await SendAsync(server, "http://example.com/me", transaction5.CookieNameValue);
            FindClaimValue(transaction6, ClaimTypes.Name).ShouldBe("hao");
        }

        [Fact]
        public async Task TwoFactorRememberCookieVerification()
        {
            var server = CreateServer(appCookieOptions => { });

            var transaction1 = await SendAsync(server, "http://example.com/createMe");
            transaction1.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Assert.Null(transaction1.SetCookie);

            var transaction2 = await SendAsync(server, "http://example.com/twofactorRememeber");
            transaction2.Response.StatusCode.ShouldBe(HttpStatusCode.OK);

            string setCookie = transaction2.SetCookie;
            setCookie.ShouldContain(IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme+"=");
            setCookie.ShouldContain("; expires=");

            var transaction3 = await SendAsync(server, "http://example.com/isTwoFactorRememebered", transaction2.CookieNameValue);
            transaction3.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private static string FindClaimValue(Transaction transaction, string claimType)
        {
            XElement claim = transaction.ResponseElement.Elements("claim").SingleOrDefault(elt => elt.Attribute("type").Value == claimType);
            if (claim == null)
            {
                return null;
            }
            return claim.Attribute("value").Value;
        }

        private static async Task<XElement> GetAuthData(TestServer server, string url, string cookie)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", cookie);

            HttpResponseMessage response2 = await server.CreateClient().SendAsync(request);
            string text = await response2.Content.ReadAsStringAsync();
            XElement me = XElement.Parse(text);
            return me;
        }

        private static TestServer CreateServer(Action<CookieAuthenticationOptions> configureAppCookie, Func<HttpContext, Task> testpath = null, Uri baseAddress = null)
        {
            var server = TestServer.Create(app =>
            {
                app.UseIdentity();
                app.Use(async (context, next) =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    var userManager = context.RequestServices.GetRequiredService<UserManager<TestUser>>();
                    var signInManager = context.RequestServices.GetRequiredService<SignInManager<TestUser>>();
                    PathString remainder;
                    if (req.Path == new PathString("/normal"))
                    {
                        res.StatusCode = 200;
                    }
                    else if (req.Path == new PathString("/createMe"))
                    {
                        var result = await userManager.CreateAsync(new TestUser("hao"), TestPassword);
                        res.StatusCode = result.Succeeded ? 200 : 500;
                    }
                    else if (req.Path == new PathString("/protected"))
                    {
                        res.StatusCode = 401;
                    }
                    else if (req.Path.StartsWithSegments(new PathString("/pwdLogin"), out remainder))
                    {
                        var isPersistent = bool.Parse(remainder.Value.Substring(1));
                        var result = await signInManager.PasswordSignInAsync("hao", TestPassword, isPersistent, false);
                        res.StatusCode = result.Succeeded ? 200 : 500;
                    }
                    else if (req.Path == new PathString("/twofactorRememeber"))
                    {
                        var user = await userManager.FindByNameAsync("hao");
                        await signInManager.RememberTwoFactorClientAsync(user);
                        res.StatusCode = 200;
                    }
                    else if (req.Path == new PathString("/isTwoFactorRememebered"))
                    {
                        var user = await userManager.FindByNameAsync("hao");
                        var result = await signInManager.IsTwoFactorClientRememberedAsync(user);
                        res.StatusCode = result ? 200 : 500;
                    }
                    else if (req.Path == new PathString("/twofactorSignIn"))
                    {
                    }
                    else if (req.Path == new PathString("/me"))
                    {
                        Describe(res, new AuthenticationResult(context.User, new AuthenticationProperties(), new AuthenticationDescription()));
                    }
                    else if (req.Path.StartsWithSegments(new PathString("/me"), out remainder))
                    {
                        var result = await context.AuthenticateAsync(remainder.Value.Substring(1));
                        Describe(res, result);
                    }
                    else if (req.Path == new PathString("/testpath") && testpath != null)
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
                services.AddIdentity<TestUser, TestRole>();
                services.AddSingleton<IUserStore<TestUser>, InMemoryUserStore<TestUser>>();
                services.AddSingleton<IRoleStore<TestRole>, InMemoryRoleStore<TestRole>>();
                services.ConfigureIdentityApplicationCookie(configureAppCookie);
            });
            server.BaseAddress = baseAddress;
            return server;
        }

        private static void Describe(HttpResponse res, AuthenticationResult result)
        {
            res.StatusCode = 200;
            res.ContentType = "text/xml";
            var xml = new XElement("xml");
            if (result != null && result.Principal != null)
            {
                xml.Add(result.Principal.Claims.Select(claim => new XElement("claim", new XAttribute("type", claim.Type), new XAttribute("value", claim.Value))));
            }
            if (result != null && result.Properties != null)
            {
                xml.Add(result.Properties.Dictionary.Select(extra => new XElement("extra", new XAttribute("type", extra.Key), new XAttribute("value", extra.Value))));
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

        private static async Task<Transaction> SendAsync(TestServer server, string uri, string cookieHeader = null, bool ajaxRequest = false)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }
            if (ajaxRequest)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            }
            var transaction = new Transaction
            {
                Request = request,
                Response = await server.CreateClient().SendAsync(request),
            };
            if (transaction.Response.Headers.Contains("Set-Cookie"))
            {
                transaction.SetCookie = transaction.Response.Headers.GetValues("Set-Cookie").SingleOrDefault();
            }
            if (!string.IsNullOrEmpty(transaction.SetCookie))
            {
                transaction.CookieNameValue = transaction.SetCookie.Split(new[] { ';' }, 2).First();
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

            public string SetCookie { get; set; }
            public string CookieNameValue { get; set; }

            public string ResponseText { get; set; }
            public XElement ResponseElement { get; set; }
        }
    }
}
