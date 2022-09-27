// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Xml.Linq;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.CookiePolicy;

// REVIEW: Should find a shared home for these potentially (Copied from Auth tests)
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
}
