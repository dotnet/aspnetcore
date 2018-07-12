// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    public class WebConfigHelpers
    {
        public static void AddDebugLogToWebConfig(string contentRoot, string filename)
        {
            var path = Path.Combine(contentRoot, "web.config");
            var webconfig = XDocument.Load(path);
            var xElement = webconfig.Descendants("aspNetCore").Single();

            var element = xElement.Descendants("handlerSettings").SingleOrDefault();
            if (element == null)
            {
                element = new XElement("handlerSettings");
                xElement.Add(element);
            }

            CreateOrSetElement(element, "debugLevel", "4");

            CreateOrSetElement(element, "debugFile", Path.Combine(contentRoot, filename));

            webconfig.Save(path);
        }

        private static void CreateOrSetElement(XElement rootElement, string name, string value)
        {
            if (rootElement.Descendants()
                .Attributes()
                .Where(attribute => attribute.Value == name)
                .Any())
            {
                return;
            }
            var element = new XElement("handlerSetting");
            element.SetAttributeValue("name", name);
            element.SetAttributeValue("value", value);
            rootElement.Add(element);
        }
    }
}
