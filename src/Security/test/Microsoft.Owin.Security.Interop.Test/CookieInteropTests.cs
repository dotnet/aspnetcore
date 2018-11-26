// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/* See https://github.com/aspnet/AspNetCore/issues/4074.

This test is was disabled as a part of changing frameworks. This test will need to be re-written using separate .NET Core and .NET Framework processes.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Testing;
using Owin;
using Xunit;

namespace Microsoft.Owin.Security.Interop
{
    public class CookiesInteropTests
    {
        [Fact]
        public async Task AspNetCoreWithInteropCookieContainsIdentity()
        {
            var identity = new ClaimsIdentity("Cookies");
            identity.AddClaim(new Claim(ClaimTypes.Name, "Alice"));

            var dataProtection = DataProtectionProvider.Create(new DirectoryInfo("..\\..\\artifacts"));
            var dataProtector = dataProtection.CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", // full name of the ASP.NET Core type
                Cookies.CookieAuthenticationDefaults.AuthenticationType, "v2");

            var interopServer = TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";

                app.UseCookieAuthentication(new Cookies.CookieAuthenticationOptions
                {
                    TicketDataFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector)),
                    CookieName = AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.CookiePrefix
                        + AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                });

                app.Run(context =>
                {
                    context.Authentication.SignIn(identity);
                    return Task.FromResult(0);
                });
            });

            var transaction = await SendAsync(interopServer, "http://example.com");

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(async context => 
                    {
                        var result = await context.AuthenticateAsync("Cookies");
                        await context.Response.WriteAsync(result.Ticket.Principal.Identity.Name);
                    });
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.DataProtectionProvider = dataProtection));
            var newServer = new AspNetCore.TestHost.TestServer(builder);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/login");
            foreach (var cookie in SetCookieHeaderValue.ParseList(transaction.SetCookie))
            {
                request.Headers.Add("Cookie", cookie.Name + "=" + cookie.Value);
            }
            var response = await newServer.CreateClient().SendAsync(request);

            Assert.Equal("Alice", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AspNetCoreWithLargeInteropCookieContainsIdentity()
        {
            var identity = new ClaimsIdentity("Cookies");
            identity.AddClaim(new Claim(ClaimTypes.Name, new string('a', 1024 * 5)));

            var dataProtection = DataProtectionProvider.Create(new DirectoryInfo("..\\..\\artifacts"));
            var dataProtector = dataProtection.CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", // full name of the ASP.NET Core type
                Cookies.CookieAuthenticationDefaults.AuthenticationType, "v2");

            var interopServer = TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";

                app.UseCookieAuthentication(new Cookies.CookieAuthenticationOptions
                {
                    TicketDataFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector)),
                    CookieName = AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.CookiePrefix
                        + AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                    CookieManager = new ChunkingCookieManager(),
                });

                app.Run(context =>
                {
                    context.Authentication.SignIn(identity);
                    return Task.FromResult(0);
                });
            });

            var transaction = await SendAsync(interopServer, "http://example.com");

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(async context =>
                    {
                        var result = await context.AuthenticateAsync("Cookies");
                        await context.Response.WriteAsync(result.Ticket.Principal.Identity.Name);
                    });
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.DataProtectionProvider = dataProtection));
            var newServer = new AspNetCore.TestHost.TestServer(builder);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/login");
            foreach (var cookie in SetCookieHeaderValue.ParseList(transaction.SetCookie))
            {
                request.Headers.Add("Cookie", cookie.Name + "=" + cookie.Value);
            }
            var response = await newServer.CreateClient().SendAsync(request);

            Assert.Equal(1024 * 5, (await response.Content.ReadAsStringAsync()).Length);
        }

        [Fact]
        public async Task InteropWithNewCookieContainsIdentity()
        {
            var user = new ClaimsPrincipal();
            var identity = new ClaimsIdentity("scheme");
            identity.AddClaim(new Claim(ClaimTypes.Name, "Alice"));
            user.AddIdentity(identity);

            var dataProtection = DataProtectionProvider.Create(new DirectoryInfo("..\\..\\artifacts"));
            var dataProtector = dataProtection.CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", // full name of the ASP.NET Core type
                Cookies.CookieAuthenticationDefaults.AuthenticationType, "v2");

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(context => context.SignInAsync("Cookies", user));
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.DataProtectionProvider = dataProtection));
            var newServer = new AspNetCore.TestHost.TestServer(builder);

            var cookies = await SendAndGetCookies(newServer, "http://example.com/login");

            var server = TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";

                app.UseCookieAuthentication(new Cookies.CookieAuthenticationOptions
                {
                    TicketDataFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector)),
                    CookieName = AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.CookiePrefix
                        + AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                });

                app.Run(async context =>
                {
                    var result = await context.Authentication.AuthenticateAsync("Cookies");
                    Describe(context.Response, result);
                });
            });

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", cookies);

            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        }

        [Fact]
        public async Task InteropWithLargeNewCookieContainsIdentity()
        {
            var user = new ClaimsPrincipal();
            var identity = new ClaimsIdentity("scheme");
            identity.AddClaim(new Claim(ClaimTypes.Name, new string('a', 1024 * 5)));
            user.AddIdentity(identity);

            var dataProtection = DataProtectionProvider.Create(new DirectoryInfo("..\\..\\artifacts"));
            var dataProtector = dataProtection.CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", // full name of the ASP.NET Core type
                Cookies.CookieAuthenticationDefaults.AuthenticationType, "v2");

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(context => context.SignInAsync("Cookies", user));
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.DataProtectionProvider = dataProtection));
            var newServer = new AspNetCore.TestHost.TestServer(builder);

            var cookies = await SendAndGetCookies(newServer, "http://example.com/login");

            var server = TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";

                app.UseCookieAuthentication(new Cookies.CookieAuthenticationOptions
                {
                    TicketDataFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector)),
                    CookieName = AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.CookiePrefix
                        + AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                    CookieManager = new ChunkingCookieManager(),
                });

                app.Run(async context =>
                {
                    var result = await context.Authentication.AuthenticateAsync("Cookies");
                    Describe(context.Response, result);
                });
            });

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", cookies);

            Assert.Equal(1024 * 5, FindClaimValue(transaction2, ClaimTypes.Name).Length);
        }

        private static async Task<IList<string>> SendAndGetCookies(AspNetCore.TestHost.TestServer server, string uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await server.CreateClient().SendAsync(request);
            if (response.Headers.Contains("Set-Cookie"))
            {
                IList<string> cookieHeaders = new List<string>();
                foreach (var cookie in SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()))
                {
                    cookieHeaders.Add(cookie.Name + "=" + cookie.Value);
                }
                return cookieHeaders;
            }
            return null;
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

        private static void Describe(IOwinResponse res, AuthenticateResult result)
        {
            res.StatusCode = 200;
            res.ContentType = "text/xml";
            var xml = new XElement("xml");
            if (result != null && result.Identity != null)
            {
                xml.Add(result.Identity.Claims.Select(claim => new XElement("claim", new XAttribute("type", claim.Type), new XAttribute("value", claim.Value))));
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

        private static async Task<Transaction> SendAsync(TestServer server, string uri, IList<string> cookieHeaders = null, bool ajaxRequest = false)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (cookieHeaders != null)
            {
                request.Headers.Add("Cookie", cookieHeaders);
            }
            if (ajaxRequest)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            }
            var transaction = new Transaction
            {
                Request = request,
                Response = await server.HttpClient.SendAsync(request),
            };
            if (transaction.Response.Headers.Contains("Set-Cookie"))
            {
                transaction.SetCookie = transaction.Response.Headers.GetValues("Set-Cookie").ToList();
            }
            if (transaction.SetCookie != null && transaction.SetCookie.Any())
            {
                transaction.CookieNameValue = transaction.SetCookie.First().Split(new[] { ';' }, 2).First();
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
            public string CookieNameValue { get; set; }

            public string ResponseText { get; set; }
            public XElement ResponseElement { get; set; }
        }

    }
}

*/
