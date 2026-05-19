// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Xml.Linq;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

internal static class TestServerExtensions
{
    public static Task<TestTransaction> SendAsync(this TestServer server, string url)
    {
        return SendAsync(server, url, cookieHeader: null);
    }

    public static Task<TestTransaction> SendAsync(this TestServer server, string url, string cookieHeader)
    {
        return SendAsync(server, new HttpRequestMessage(HttpMethod.Get, url), cookieHeader);
    }

    public static async Task<TestTransaction> SendAsync(this TestServer server, HttpRequestMessage request, string cookieHeader)
    {
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
