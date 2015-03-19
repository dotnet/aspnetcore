// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication.Cookies
{
    public class CookieMiddlewareTests
    {
        [Fact]
        public async Task NormalRequestPassesThrough()
        {
            TestServer server = CreateServer(options =>
            {
            });
            HttpResponseMessage response = await server.CreateClient().GetAsync("http://example.com/normal");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ProtectedRequestShouldRedirectToLoginOnlyWhenAutomatic(bool auto)
        {
            TestServer server = CreateServer(options =>
            {
                options.LoginPath = new PathString("/login");
                options.AutomaticAuthentication = auto;
            });

            Transaction transaction = await SendAsync(server, "http://example.com/protected");

            transaction.Response.StatusCode.ShouldBe(auto ? HttpStatusCode.Redirect : HttpStatusCode.Unauthorized);
            if (auto)
            {
                Uri location = transaction.Response.Headers.Location;
                location.LocalPath.ShouldBe("/login");
                location.Query.ShouldBe("?ReturnUrl=%2Fprotected");
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ProtectedCustomRequestShouldRedirectToCustomLogin(bool auto)
        {
            TestServer server = CreateServer(options =>
            {
                options.LoginPath = new PathString("/login");
                options.AutomaticAuthentication = auto;
            });

            Transaction transaction = await SendAsync(server, "http://example.com/protected/CustomRedirect");

            transaction.Response.StatusCode.ShouldBe(auto ? HttpStatusCode.Redirect : HttpStatusCode.Unauthorized);
            if (auto)
            {
                Uri location = transaction.Response.Headers.Location;
                location.ToString().ShouldBe("/CustomRedirect");
            }
        }

        private Task SignInAsAlice(HttpContext context)
        {
            context.Response.SignIn("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                new AuthenticationProperties());
            return Task.FromResult<object>(null);
        }

        private Task SignInAsWrong(HttpContext context)
        {
            context.Response.SignIn("Oops",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                new AuthenticationProperties());
            return Task.FromResult<object>(null);
        }

        private Task SignOutAsWrong(HttpContext context)
        {
            context.Response.SignOut("Oops");
            return Task.FromResult<object>(null);
        }

        [Fact]
        public async Task SignInCausesDefaultCookieToBeCreated()
        {
            TestServer server = CreateServer(options =>
            {
                options.LoginPath = new PathString("/login");
                options.CookieName = "TestCookie";
            }, SignInAsAlice);

            Transaction transaction = await SendAsync(server, "http://example.com/testpath");

            string setCookie = transaction.SetCookie;
            setCookie.ShouldStartWith("TestCookie=");
            setCookie.ShouldContain("; path=/");
            setCookie.ShouldContain("; HttpOnly");
            setCookie.ShouldNotContain("; expires=");
            setCookie.ShouldNotContain("; domain=");
            setCookie.ShouldNotContain("; secure");
        }

        [Fact]
        public async Task SignInWrongAuthTypeThrows()
        {
            TestServer server = CreateServer(options =>
            {
                options.LoginPath = new PathString("/login");
                options.CookieName = "TestCookie";
            }, SignInAsWrong);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await SendAsync(server, "http://example.com/testpath"));
        }

        [Fact]
        public async Task SignOutWrongAuthTypeThrows()
        {
            TestServer server = CreateServer(options =>
            {
                options.LoginPath = new PathString("/login");
                options.CookieName = "TestCookie";
            }, SignOutAsWrong);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await SendAsync(server, "http://example.com/testpath"));
        }

        [Theory]
        [InlineData(CookieSecureOption.Always, "http://example.com/testpath", true)]
        [InlineData(CookieSecureOption.Always, "https://example.com/testpath", true)]
        [InlineData(CookieSecureOption.Never, "http://example.com/testpath", false)]
        [InlineData(CookieSecureOption.Never, "https://example.com/testpath", false)]
        [InlineData(CookieSecureOption.SameAsRequest, "http://example.com/testpath", false)]
        [InlineData(CookieSecureOption.SameAsRequest, "https://example.com/testpath", true)]
        public async Task SecureSignInCausesSecureOnlyCookieByDefault(
            CookieSecureOption cookieSecureOption,
            string requestUri,
            bool shouldBeSecureOnly)
        {
            TestServer server = CreateServer(options =>
            {
                options.LoginPath = new PathString("/login");
                options.CookieName = "TestCookie";
                options.CookieSecure = cookieSecureOption;
            }, SignInAsAlice);

            Transaction transaction = await SendAsync(server, requestUri);
            string setCookie = transaction.SetCookie;

            if (shouldBeSecureOnly)
            {
                setCookie.ShouldContain("; secure");
            }
            else
            {
                setCookie.ShouldNotContain("; secure");
            }
        }

        [Fact]
        public async Task CookieOptionsAlterSetCookieHeader()
        {
            TestServer server1 = CreateServer(options =>
            {
                options.CookieName = "TestCookie";
                options.CookiePath = "/foo";
                options.CookieDomain = "another.com";
                options.CookieSecure = CookieSecureOption.Always;
                options.CookieHttpOnly = true;
            }, SignInAsAlice, new Uri("http://example.com/base"));

            Transaction transaction1 = await SendAsync(server1, "http://example.com/base/testpath");

            string setCookie1 = transaction1.SetCookie;

            setCookie1.ShouldContain("TestCookie=");
            setCookie1.ShouldContain(" path=/foo");
            setCookie1.ShouldContain(" domain=another.com");
            setCookie1.ShouldContain(" secure");
            setCookie1.ShouldContain(" HttpOnly");

            TestServer server2 = CreateServer(options =>
            {
                options.CookieName = "SecondCookie";
                options.CookieSecure = CookieSecureOption.Never;
                options.CookieHttpOnly = false;
            }, SignInAsAlice, new Uri("http://example.com/base"));

            Transaction transaction2 = await SendAsync(server2, "http://example.com/base/testpath");

            string setCookie2 = transaction2.SetCookie;

            setCookie2.ShouldContain("SecondCookie=");
            setCookie2.ShouldContain(" path=/base");
            setCookie2.ShouldNotContain(" domain=");
            setCookie2.ShouldNotContain(" secure");
            setCookie2.ShouldNotContain(" HttpOnly");
        }

        [Fact]
        public async Task CookieContainsIdentity()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
            }, SignInAsAlice);

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            FindClaimValue(transaction2, ClaimTypes.Name).ShouldBe("Alice");
        }

        [Fact]
        public async Task CookieAppliesClaimsTransform()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
            }, 
            SignInAsAlice, 
            baseAddress: null, 
            claimsTransform: o => o.Transformation = (p =>
            {
                if (!p.Identities.Any(i => i.AuthenticationType == "xform"))
                {
                    // REVIEW: Xform runs twice, once on Authenticate, and then once from the middleware
                    var id = new ClaimsIdentity("xform");
                    id.AddClaim(new Claim("xform", "yup"));
                    p.AddIdentity(id);
                }
                return p;
            }));

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            FindClaimValue(transaction2, ClaimTypes.Name).ShouldBe("Alice");
            FindClaimValue(transaction2, "xform").ShouldBe("yup");

        }

        [Fact]
        public async Task CookieStopsWorkingAfterExpiration()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            }, SignInAsAlice);

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(7));

            Transaction transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(7));

            Transaction transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            transaction2.SetCookie.ShouldBe(null);
            FindClaimValue(transaction2, ClaimTypes.Name).ShouldBe("Alice");
            transaction3.SetCookie.ShouldBe(null);
            FindClaimValue(transaction3, ClaimTypes.Name).ShouldBe("Alice");
            transaction4.SetCookie.ShouldBe(null);
            FindClaimValue(transaction4, ClaimTypes.Name).ShouldBe(null);
        }

        [Fact]
        public async Task CookieExpirationCanBeOverridenInSignin()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            },
            context =>
            {
                context.Response.SignIn("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                    new AuthenticationProperties() { ExpiresUtc = clock.UtcNow.Add(TimeSpan.FromMinutes(5)) });
                    return Task.FromResult<object>(null);
            });

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(3));

            Transaction transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(3));

            Transaction transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            transaction2.SetCookie.ShouldBe(null);
            FindClaimValue(transaction2, ClaimTypes.Name).ShouldBe("Alice");
            transaction3.SetCookie.ShouldBe(null);
            FindClaimValue(transaction3, ClaimTypes.Name).ShouldBe("Alice");
            transaction4.SetCookie.ShouldBe(null);
            FindClaimValue(transaction4, ClaimTypes.Name).ShouldBe(null);
        }

        [Fact]
        public async Task CookieExpirationCanBeOverridenInEvent()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
                options.Notifications = new CookieAuthenticationNotifications()
                {
                    OnResponseSignIn = context =>
                    {
                        context.Properties.ExpiresUtc = clock.UtcNow.Add(TimeSpan.FromMinutes(5));
                    }
                };
            }, SignInAsAlice);

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(3));

            Transaction transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(3));

            Transaction transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            transaction2.SetCookie.ShouldBe(null);
            FindClaimValue(transaction2, ClaimTypes.Name).ShouldBe("Alice");
            transaction3.SetCookie.ShouldBe(null);
            FindClaimValue(transaction3, ClaimTypes.Name).ShouldBe("Alice");
            transaction4.SetCookie.ShouldBe(null);
            FindClaimValue(transaction4, ClaimTypes.Name).ShouldBe(null);
        }

        [Fact]
        public async Task CookieIsRenewedWithSlidingExpiration()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = true;
            }, SignInAsAlice);

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(4));

            Transaction transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(4));

            // transaction4 should arrive with a new SetCookie value
            Transaction transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(4));

            Transaction transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);

            transaction2.SetCookie.ShouldBe(null);
            FindClaimValue(transaction2, ClaimTypes.Name).ShouldBe("Alice");
            transaction3.SetCookie.ShouldBe(null);
            FindClaimValue(transaction3, ClaimTypes.Name).ShouldBe("Alice");
            transaction4.SetCookie.ShouldNotBe(null);
            FindClaimValue(transaction4, ClaimTypes.Name).ShouldBe("Alice");
            transaction5.SetCookie.ShouldBe(null);
            FindClaimValue(transaction5, ClaimTypes.Name).ShouldBe("Alice");
        }

        [Fact]
        public async Task AjaxRedirectsAsExtraHeaderOnTwoHundred()
        {
            TestServer server = CreateServer(options =>
            {
                options.LoginPath = new PathString("/login");
                options.AutomaticAuthentication = true;
            });

            Transaction transaction = await SendAsync(server, "http://example.com/protected", ajaxRequest: true);

            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var responded = transaction.Response.Headers.GetValues("X-Responded-JSON");

            responded.Count().ShouldBe(1);
            responded.Single().ShouldContain("\"location\"");
        }

        [Fact]
        public async Task CookieUsesPathBaseByDefault()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
            },
            context =>
            {
                Assert.Equal(new PathString("/base"), context.Request.PathBase);
                context.Response.SignIn("Cookies", 
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))));
                return Task.FromResult<object>(null);
            },
            new Uri("http://example.com/base"));

            Transaction transaction1 = await SendAsync(server, "http://example.com/base/testpath");
            Assert.True(transaction1.SetCookie.Contains("path=/base"));
        }

        [Fact]
        public async Task CookieTurns401To403IfAuthenticated()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
            }, 
            SignInAsAlice);

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/unauthorized", transaction1.CookieNameValue);

            transaction2.Response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CookieTurns401ToAccessDeniedWhenSetAndIfAuthenticated()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
                options.AccessDeniedPath = new PathString("/accessdenied");
            },
            SignInAsAlice);

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/unauthorized", transaction1.CookieNameValue);

            transaction2.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

            Uri location = transaction2.Response.Headers.Location;
            location.LocalPath.ShouldBe("/accessdenied");
        }

        [Fact]
        public async Task CookieDoesNothingTo401IfNotAuthenticated()
        {
            var clock = new TestClock();
            TestServer server = CreateServer(options =>
            {
                options.SystemClock = clock;
            });

            Transaction transaction1 = await SendAsync(server, "http://example.com/testpath");

            Transaction transaction2 = await SendAsync(server, "http://example.com/unauthorized", transaction1.CookieNameValue);

            transaction2.Response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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

        private static TestServer CreateServer(Action<CookieAuthenticationOptions> configureOptions, Func<HttpContext, Task> testpath = null, Uri baseAddress = null, Action<ClaimsTransformationOptions> claimsTransform = null)
        {
            var server = TestServer.Create(app =>
            {
                app.UseCookieAuthentication(configureOptions);
                if (claimsTransform != null)
                {
                    app.UseClaimsTransformation();
                }
                app.Use(async (context, next) =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    PathString remainder;
                    if (req.Path == new PathString("/normal"))
                    {
                        res.StatusCode = 200;
                    }
                    else if (req.Path == new PathString("/protected"))
                    {
                        res.StatusCode = 401;
                    }
                    else if (req.Path == new PathString("/unauthorized"))
                    {
                        // Simulate Authorization failure 
                        var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        res.Challenge(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                    else if (req.Path == new PathString("/protected/CustomRedirect"))
                    {
                        context.Response.Challenge(new AuthenticationProperties() { RedirectUri = "/CustomRedirect" });
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
                services.AddDataProtection();
                if (claimsTransform != null)
                {
                    services.ConfigureClaimsTransformation(claimsTransform);
                }
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
