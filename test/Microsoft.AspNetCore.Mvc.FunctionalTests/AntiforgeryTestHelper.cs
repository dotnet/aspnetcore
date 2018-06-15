// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public static class AntiforgeryTestHelper
    {
        public static string RetrieveAntiforgeryToken(string htmlContent)
            => RetrieveAntiforgeryToken(htmlContent, actionUrl: string.Empty);

        public static string RetrieveAntiforgeryToken(string htmlContent, string actionUrl)
        {
            var parser = new HtmlParser();
            var htmlDocument = parser.Parse(htmlContent);

            return RetrieveAntiforgeryToken(htmlDocument);
        }

        public static string RetrieveAntiforgeryToken(IHtmlDocument htmlDocument)
        {
            var hiddenInputs = htmlDocument.QuerySelectorAll("form input[type=hidden]");
            foreach (var input in hiddenInputs)
            {
                if (!input.HasAttribute("name"))
                {
                    continue;
                }

                var name = input.GetAttribute("name");
                if (name == "__RequestVerificationToken" || name == "HtmlEncode[[__RequestVerificationToken]]")
                {
                    return input.GetAttribute("value");
                }
            }

            throw new Exception($"Antiforgery token could not be located in {htmlDocument.Source.Text}.");
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
}