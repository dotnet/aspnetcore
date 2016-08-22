// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.Authentication.Tests.OpenIdConnect
{
    internal class TestTransaction
    {
        public static Task<TestTransaction> SendAsync(TestServer server, string url)
        {
            return SendAsync(server, url, cookieHeader: null);
        }

        public static async Task<TestTransaction> SendAsync(TestServer server, string uri, string cookieHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }

            var transaction = new TestTransaction
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
                    var authCookie = SetCookie.SingleOrDefault(c => c.Contains(".AspNetCore.Cookie="));
                    if (authCookie != null)
                    {
                        return authCookie.Substring(0, authCookie.IndexOf(';'));
                    }
                }

                return null;
            }
        }
    }
}