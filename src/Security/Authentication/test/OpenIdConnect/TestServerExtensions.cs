// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect
{
    internal static class TestServerExtensions
    {
        public static Task<TestTransaction> SendAsync(this TestServer server, string url)
        {
            return SendAsync(server, url, cookieHeader: null);
        }

        public static async Task<TestTransaction> SendAsync(this TestServer server, string uri, string cookieHeader)
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
    }
}
