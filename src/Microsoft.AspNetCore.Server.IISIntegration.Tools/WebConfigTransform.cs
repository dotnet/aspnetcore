// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tools
{
    public static class WebConfigTransform
    {
        public static XDocument Transform(XDocument webConfig, string appName, bool configureForAzure)
        {
            const string HandlersElementName = "handlers";
            const string aspNetCoreElementName = "aspNetCore";

            webConfig = webConfig == null || webConfig.Root.Name.LocalName != "configuration"
                ? XDocument.Parse("<configuration />")
                : webConfig;

            var webServerSection = GetOrCreateChild(webConfig.Root, "system.webServer");

            TransformHandlers(GetOrCreateChild(webServerSection, HandlersElementName));
            TransformAspNetCore(GetOrCreateChild(webServerSection, aspNetCoreElementName), appName, configureForAzure);

            // make sure that the aspNetCore element is after handlers element
            var aspNetCoreElement = webServerSection.Element(HandlersElementName)
                .ElementsBeforeSelf(aspNetCoreElementName).SingleOrDefault();
            if (aspNetCoreElement != null)
            {
                aspNetCoreElement.Remove();
                webServerSection.Element(HandlersElementName).AddAfterSelf(aspNetCoreElement);
            }

            return webConfig;
        }

        private static void TransformHandlers(XElement handlersElement)
        {
            var aspNetCoreElement =
                handlersElement.Elements("add")
                    .FirstOrDefault(e => string.Equals((string)e.Attribute("name"), "aspnetcore", StringComparison.OrdinalIgnoreCase));

            if (aspNetCoreElement == null)
            {
                aspNetCoreElement = new XElement("add");
                handlersElement.Add(aspNetCoreElement);
            }

            aspNetCoreElement.SetAttributeValue("name", "aspNetCore");
            SetAttributeValueIfEmpty(aspNetCoreElement, "path", "*");
            SetAttributeValueIfEmpty(aspNetCoreElement, "verb", "*");
            SetAttributeValueIfEmpty(aspNetCoreElement, "modules", "AspNetCoreModule");
            SetAttributeValueIfEmpty(aspNetCoreElement, "resourceType", "Unspecified");
        }

        private static void TransformAspNetCore(XElement aspNetCoreElement, string appName, bool configureForAzure)
        {
            var appPath = Path.Combine(configureForAzure ? @"%home%\site" : "..", appName);
            var logPath = Path.Combine(configureForAzure ? @"\\?\%home%\LogFiles" : @"..\logs", "stdout.log");

            aspNetCoreElement.SetAttributeValue("processPath", appPath);
            SetAttributeValueIfEmpty(aspNetCoreElement, "stdoutLogEnabled", "false");
            SetAttributeValueIfEmpty(aspNetCoreElement, "stdoutLogFile", logPath);
            SetAttributeValueIfEmpty(aspNetCoreElement, "startupTimeLimit", "3600");

            AddApplicationBase(aspNetCoreElement);
        }

        private static void AddApplicationBase(XElement aspNetCoreElement)
        {
            const string contentRootKeyName = "ASPNETCORE_CONTENTROOT";

            var envVariables = GetOrCreateChild(aspNetCoreElement, "environmentVariables");
            var appBaseElement = envVariables.Elements("environmentVariable").SingleOrDefault(e =>
                string.Equals((string)e.Attribute("name"), contentRootKeyName, StringComparison.CurrentCultureIgnoreCase));

            if (appBaseElement == null)
            {
                appBaseElement = new XElement("environmentVariable", new XAttribute("name", contentRootKeyName));
                envVariables.AddFirst(appBaseElement);
            }

            appBaseElement.SetAttributeValue("value", ".");
        }

        private static XElement GetOrCreateChild(XElement parent, string childName)
        {
            var childElement = parent.Element(childName);
            if (childElement == null)
            {
                childElement = new XElement(childName);
                parent.Add(childElement);
            }
            return childElement;
        }

        private static void SetAttributeValueIfEmpty(XElement element, string attributeName, string value)
        {
            element.SetAttributeValue(attributeName, (string)element.Attribute(attributeName) ?? value);
        }
    }
}