// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNet.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Cookies.Interop;
using Microsoft.Owin.Testing;
using Owin;
using Xunit;

namespace Microsoft.AspNet.CookiePolicy.Test
{
    public class TicketInteropTests
    {
        [Fact]
        public void NewSerializerCanReadInteropTicket()
        {
            var identity = new ClaimsIdentity("scheme");
            identity.AddClaim(new Claim("Test", "Value"));

            var expires = DateTime.Today;
            var issued = new DateTime(1979, 11, 11);
            var properties = new Owin.Security.AuthenticationProperties();
            properties.IsPersistent = true;
            properties.RedirectUri = "/redirect";
            properties.Dictionary["key"] = "value";
            properties.ExpiresUtc = expires;
            properties.IssuedUtc = issued;

            var interopTicket = new Owin.Security.AuthenticationTicket(identity, properties);
            var interopSerializer = new AspNetTicketSerializer();

            var bytes = interopSerializer.Serialize(interopTicket);

            var newSerializer = new TicketSerializer();
            var newTicket = newSerializer.Deserialize(bytes);

            Assert.NotNull(newTicket);
            Assert.Equal(1, newTicket.Principal.Identities.Count());
            var newIdentity = newTicket.Principal.Identity as ClaimsIdentity;
            Assert.NotNull(newIdentity);
            Assert.Equal("scheme", newIdentity.AuthenticationType);
            Assert.True(newIdentity.HasClaim(c => c.Type == "Test" && c.Value == "Value"));
            Assert.NotNull(newTicket.Properties);
            Assert.True(newTicket.Properties.IsPersistent);
            Assert.Equal("/redirect", newTicket.Properties.RedirectUri);
            Assert.Equal("value", newTicket.Properties.Items["key"]);
            Assert.Equal(expires, newTicket.Properties.ExpiresUtc);
            Assert.Equal(issued, newTicket.Properties.IssuedUtc);
        }

        [Fact]
        public void InteropSerializerCanReadNewTicket()
        {
            var user = new ClaimsPrincipal();
            var identity = new ClaimsIdentity("scheme");
            identity.AddClaim(new Claim("Test", "Value"));
            user.AddIdentity(identity);

            var expires = DateTime.Today;
            var issued = new DateTime(1979, 11, 11);
            var properties = new Http.Authentication.AuthenticationProperties();
            properties.IsPersistent = true;
            properties.RedirectUri = "/redirect";
            properties.Items["key"] = "value";
            properties.ExpiresUtc = expires;
            properties.IssuedUtc = issued;

            var newTicket = new AuthenticationTicket(user, properties, "scheme");
            var newSerializer = new TicketSerializer();

            var bytes = newSerializer.Serialize(newTicket);

            var interopSerializer = new AspNetTicketSerializer();
            var interopTicket = interopSerializer.Deserialize(bytes);

            Assert.NotNull(interopTicket);
            var newIdentity = interopTicket.Identity;
            Assert.NotNull(newIdentity);
            Assert.Equal("scheme", newIdentity.AuthenticationType);
            Assert.True(newIdentity.HasClaim(c => c.Type == "Test" && c.Value == "Value"));
            Assert.NotNull(interopTicket.Properties);
            Assert.True(interopTicket.Properties.IsPersistent);
            Assert.Equal("/redirect", interopTicket.Properties.RedirectUri);
            Assert.Equal("value", interopTicket.Properties.Dictionary["key"]);
            Assert.Equal(expires, interopTicket.Properties.ExpiresUtc);
            Assert.Equal(issued, interopTicket.Properties.IssuedUtc);
        }

        [Fact]
        public async Task AspNet5WithInteropCookieContainsIdentity()
        {
            var identity = new ClaimsIdentity("Cookies");
            identity.AddClaim(new Claim(ClaimTypes.Name, "Alice"));

            var dataProtection = new DataProtection.DataProtectionProvider(new DirectoryInfo("."));

            var interopServer = TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";
                app.UseCookieAuthentication(new CookieAuthenticationOptions(), dataProtection);
                app.Run(context =>
                {
                    context.Authentication.SignIn(identity);
                    return Task.FromResult(0);
                });
            });

            var transaction = await SendAsync(interopServer, "http://example.com");

            var newServer = TestHost.TestServer.Create(app =>
            {
                app.UseCookieAuthentication(options => options.DataProtectionProvider = dataProtection);
                app.Run(async context => 
                {
                    var result = await context.Authentication.AuthenticateAsync("Cookies");
                    await context.Response.WriteAsync(result.Identity.Name);
                });
            }, services => services.AddAuthentication());

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

            var dataProtection = new DataProtection.DataProtectionProvider(new DirectoryInfo("."));

            var newServer = TestHost.TestServer.Create(app =>
            {
                app.UseCookieAuthentication(options => options.DataProtectionProvider = dataProtection);
                app.Run(context => context.Authentication.SignInAsync("Cookies", user));
            }, services => services.AddAuthentication());

            var cookie = await SendAndGetCookie(newServer, "http://example.com/login");

            var server = TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";
                app.UseCookieAuthentication(new CookieAuthenticationOptions(), dataProtection);
                app.Run(async context =>
                {
                    var result = await context.Authentication.AuthenticateAsync("Cookies");
                    Describe(context.Response, result);
                });
            });

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", cookie);

            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        }

        private static async Task<string> SendAndGetCookie(TestHost.TestServer server, string uri)
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


