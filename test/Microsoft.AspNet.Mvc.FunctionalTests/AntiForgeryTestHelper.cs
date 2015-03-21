// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public static class AntiForgeryTestHelper
    {
        public static string RetrieveAntiForgeryToken(string htmlContent, string actionUrl)
        {
            return RetrieveAntiForgeryTokens(
                htmlContent,
                attribute => attribute.Value.EndsWith(actionUrl, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        public static IEnumerable<string> RetrieveAntiForgeryTokens(
            string htmlContent,
            Func<XAttribute, bool> predicate = null)
        {
            predicate = predicate ?? (_ => true);
            htmlContent = "<Root>" + htmlContent + "</Root>";
            var reader = new StringReader(htmlContent);
            var htmlDocument = XDocument.Load(reader);

            foreach (var form in htmlDocument.Descendants("form"))
            {
                foreach (var attribute in form.Attributes())
                {
                    if (string.Equals(attribute.Name.LocalName, "action", StringComparison.OrdinalIgnoreCase)
                        && predicate(attribute))
                    {
                        foreach (var input in form.Descendants("input"))
                        {
                            if (input.Attribute("name") != null &&
                                input.Attribute("type") != null &&
                                input.Attribute("name").Value == "__RequestVerificationToken" &&
                                input.Attribute("type").Value == "hidden")
                            {
                                yield return input.Attributes("value").First().Value;
                            }
                        }
                    }
                }
            }
        }

        public static CookieMetadata RetrieveAntiForgeryCookie(HttpResponseMessage response)
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