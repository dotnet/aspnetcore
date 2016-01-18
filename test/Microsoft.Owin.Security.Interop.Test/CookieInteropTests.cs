// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Testing;
using Owin;
using Xunit;

namespace Microsoft.Owin.Security.Interop
{
    public class CookiesInteropTests
    {
        [Fact]
        public async Task AspNet5WithInteropCookieContainsIdentity()
        {
            var identity = new ClaimsIdentity("Cookies");
            identity.AddClaim(new Claim(ClaimTypes.Name, "Alice"));

            var dataProtection = new DataProtectionProvider(new DirectoryInfo("..\\..\\artifacts"));
            var dataProtector = dataProtection.CreateProtector(
                "Microsoft.AspNet.Authentication.Cookies.CookieAuthenticationMiddleware", // full name of the ASP.NET 5 type
                CookieAuthenticationDefaults.AuthenticationType, "v2");

            var interopServer = TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";

                app.UseCookieAuthentication(new Cookies.CookieAuthenticationOptions
                {
                    TicketDataFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector))
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
                    app.UseCookieAuthentication(new AspNet.Builder.CookieAuthenticationOptions
                    {
                        DataProtectionProvider = dataProtection
                    });
                    app.Run(async context => 
                    {
                        var result = await context.Authentication.AuthenticateAsync("Cookies");
                        await context.Response.WriteAsync(result.Identity.Name);
                    });
                })
                .ConfigureServices(services => services.AddAuthentication());
            var newServer = new AspNet.TestHost.TestServer(builder);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/login");
            request.Headers.Add("Cookie", transaction.SetCookie.Split(new[] { ';' }, 2).First());
            var response = await newServer.CreateClient().SendAsync(request);

            Assert.Equal("Alice", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InteropWithNewCookieContainsIdentity()
        {
            var user = new ClaimsPrincipal();
            var identity = new ClaimsIdentity("scheme");
            identity.AddClaim(new Claim(ClaimTypes.Name, "Alice"));
            user.AddIdentity(identity);

            var dataProtection = new DataProtectionProvider(new DirectoryInfo("..\\..\\artifacts"));
            var dataProtector = dataProtection.CreateProtector(
                "Microsoft.AspNet.Authentication.Cookies.CookieAuthenticationMiddleware", // full name of the ASP.NET 5 type
                CookieAuthenticationDefaults.AuthenticationType, "v2");

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication(new AspNet.Builder.CookieAuthenticationOptions
                    {
                        DataProtectionProvider = dataProtection
                    });
                    app.Run(context => context.Authentication.SignInAsync("Cookies", user));
                })
                .ConfigureServices(services => services.AddAuthentication());
            var newServer = new AspNet.TestHost.TestServer(builder);

            var cookie = await SendAndGetCookie(newServer, "http://example.com/login");

            var server = TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";

                app.UseCookieAuthentication(new Owin.Security.Cookies.CookieAuthenticationOptions
                {
                    TicketDataFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector))
                });

                app.Run(async context =>
                {
                    var result = await context.Authentication.AuthenticateAsync("Cookies");
                    Describe(context.Response, result);
                });
            });

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", cookie);

            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        }

        private static async Task<string> SendAndGetCookie(AspNet.TestHost.TestServer server, string uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await server.CreateClient().SendAsync(request);
            if (response.Headers.Contains("Set-Cookie"))
            {
                return response.Headers.GetValues("Set-Cookie").ToList().First();
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

        private static void Describe(IOwinResponse res, Owin.Security.AuthenticateResult result)
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
                Response = await server.HttpClient.SendAsync(request),
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

