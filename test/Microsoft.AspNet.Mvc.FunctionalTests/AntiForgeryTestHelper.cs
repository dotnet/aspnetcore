// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            htmlContent = "<Root>" + htmlContent + "</Root>";
            var reader = new StringReader(htmlContent);
            var htmlDocument = XDocument.Load(reader);
            foreach (var form in htmlDocument.Descendants("form"))
            {
                foreach (var attribute in form.Attributes())
                {
                    if (string.Equals(attribute.Name.LocalName, "action", StringComparison.OrdinalIgnoreCase) &&
                        attribute.Value.EndsWith(actionUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var input in form.Descendants("input"))
                        {
                            if (input.Attributes("name").First().Value == "__RequestVerificationToken" && 
                                input.Attributes("type").First().Value == "hidden")
                            {
                                return input.Attributes("value").First().Value;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static string RetrieveAntiForgeryCookie(HttpResponseMessage response)
        {
            var setCookieArray = response.Headers.GetValues("Set-Cookie").ToArray();
            return setCookieArray[0].Split(';')
                                    .Where(headerValue => headerValue.StartsWith("__RequestVerificationToken"))
                                    .First()
                                    .Split('=')[1];
        }
    }
}