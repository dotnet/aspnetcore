// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Xml;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public static class AntiForgeryTestHelper
    {
        public static string RetrieveAntiForgeryToken(string htmlContent, string actionUrl)
        {
            int startSearchIndex = 0;

            while (startSearchIndex < htmlContent.Length)
            {
                var formStartIndex = htmlContent.IndexOf("<form", startSearchIndex, StringComparison.OrdinalIgnoreCase);
                var formEndIndex = htmlContent.IndexOf("</form>", startSearchIndex, StringComparison.OrdinalIgnoreCase);

                if (formStartIndex == -1 || formEndIndex == -1)
                {
                    //Unable to find the form start or end - finish the search
                    return null;
                }

                formEndIndex = formEndIndex + "</form>".Length;
                startSearchIndex = formEndIndex + 1;

                var htmlDocument = new XmlDocument();
                htmlDocument.LoadXml(htmlContent.Substring(formStartIndex, formEndIndex - formStartIndex));
                foreach (XmlAttribute attribute in htmlDocument.DocumentElement.Attributes)
                {
                    if (string.Equals(attribute.Name, "action", StringComparison.OrdinalIgnoreCase) && attribute.Value.EndsWith(actionUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (XmlNode input in htmlDocument.GetElementsByTagName("input"))
                        {
                            if (input.Attributes["name"].Value == "__RequestVerificationToken" && input.Attributes["type"].Value == "hidden")
                            {
                                return input.Attributes["value"].Value;
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