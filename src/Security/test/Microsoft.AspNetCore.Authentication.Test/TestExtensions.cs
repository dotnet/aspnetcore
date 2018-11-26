// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.Authentication
{
    public static class TestExtensions
    {
        public const string CookieAuthenticationScheme = "External";

        public static async Task<Transaction> SendAsync(this TestServer server, string uri, string cookieHeader = null)
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

        public static void Describe(this HttpResponse res, ClaimsPrincipal principal)
        {
            res.StatusCode = 200;
            res.ContentType = "text/xml";
            var xml = new XElement("xml");
            if (principal != null)
            {
                foreach (var identity in principal.Identities)
                {
                    xml.Add(identity.Claims.Select(claim => 
                        new XElement("claim", new XAttribute("type", claim.Type), 
                        new XAttribute("value", claim.Value), 
                        new XAttribute("issuer", claim.Issuer))));
                }
            }
            var xmlBytes = Encoding.UTF8.GetBytes(xml.ToString());
            res.Body.Write(xmlBytes, 0, xmlBytes.Length);
        }

        public static void Describe(this HttpResponse res, IEnumerable<AuthenticationToken> tokens)
        {
            res.StatusCode = 200;
            res.ContentType = "text/xml";
            var xml = new XElement("xml");
            if (tokens != null)
            {
                foreach (var token in tokens)
                {
                    xml.Add(new XElement("token", new XAttribute("name", token.Name),
                        new XAttribute("value", token.Value)));
                }
            }
            var xmlBytes = Encoding.UTF8.GetBytes(xml.ToString());
            res.Body.Write(xmlBytes, 0, xmlBytes.Length);
        }

    }
}
