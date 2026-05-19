// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Parser.Html;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public static class AntiforgeryTestHelper
{
    public static string RetrieveAntiforgeryToken(string htmlContent)
        => RetrieveAntiforgeryToken(htmlContent, actionUrl: string.Empty);

    public static string RetrieveAntiforgeryToken(string htmlContent, string actionUrl)
    {
        var parser = new HtmlParser();
        var htmlDocument = parser.Parse(htmlContent);

        return htmlDocument.RetrieveAntiforgeryToken();
    }

    public static CookieMetadata RetrieveAntiforgeryCookie(HttpResponseMessage response)
    {
        var setCookieArray = response.Headers.GetValues("Set-Cookie").ToArray();
        var cookie = setCookieArray[0].Split(';').First().Split('=');
        var cookieKey = cookie[0];
        var cookieData = cookie[1];

        return new CookieMetadata()
        {
            Key = cookieKey,
            Value = cookieData
        };
    }

    public class CookieMetadata
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
