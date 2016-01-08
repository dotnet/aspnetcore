using System;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNet.Tools.PublishIIS
{
    public static class WebConfigTransform
    {
        public static XDocument Transform(XDocument webConfig, string appName)
        {
            const string HandlersElementName = "handlers";
            const string httpPlatformElementName = "httpPlatform";

            webConfig = webConfig == null || webConfig.Root.Name.LocalName != "configuration"
                ? XDocument.Parse("<configuration />")
                : webConfig;

            var webServerSection = GetOrCreateChild(webConfig.Root, "system.webServer");

            TransformHandlers(GetOrCreateChild(webServerSection, HandlersElementName));
            TransformHttpPlatform(GetOrCreateChild(webServerSection, httpPlatformElementName), appName);

            // make sure that the httpPlatform element is after handlers element
            var httpPlatformElement = webServerSection.Element(HandlersElementName)
                .ElementsBeforeSelf(httpPlatformElementName).SingleOrDefault();
            if (httpPlatformElement != null)
            {
                httpPlatformElement.Remove();
                webServerSection.Element(HandlersElementName).AddAfterSelf(httpPlatformElement);
            }

            return webConfig;
        }

        private static void TransformHandlers(XElement handlersElement)
        {
            var platformHandlerElement =
                handlersElement.Elements("add")
                    .FirstOrDefault(e => string.Equals((string)e.Attribute("name"), "httpplatformhandler", StringComparison.OrdinalIgnoreCase));

            if (platformHandlerElement == null)
            {
                platformHandlerElement = new XElement("add");
                handlersElement.Add(platformHandlerElement);
            }

            platformHandlerElement.SetAttributeValue("name", "httpPlatformHandler");
            SetAttributeValueIfEmpty(platformHandlerElement, "path", "*");
            SetAttributeValueIfEmpty(platformHandlerElement, "verb", "*");
            SetAttributeValueIfEmpty(platformHandlerElement, "modules", "httpPlatformHandler");
            SetAttributeValueIfEmpty(platformHandlerElement, "resourceType", "Unspecified");
        }

        private static void TransformHttpPlatform(XElement httpPlatformElement, string appName)
        {
            httpPlatformElement.SetAttributeValue("processPath", $@"..\wwwroot\{appName}");
            SetAttributeValueIfEmpty(httpPlatformElement, "stdoutLogEnabled", "false");
            SetAttributeValueIfEmpty(httpPlatformElement, "startupTimeLimit", "3600");
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