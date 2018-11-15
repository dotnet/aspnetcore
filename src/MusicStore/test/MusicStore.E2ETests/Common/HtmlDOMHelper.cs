using System;
using System.Xml;

namespace E2ETests
{
    public class HtmlDOMHelper
    {
        public static string RetrieveAntiForgeryToken(string htmlContent, string actionUrl)
        {
            int startSearchIndex = 0;

            while (startSearchIndex < htmlContent.Length)
            {
                var antiForgeryToken = RetrieveAntiForgeryToken(htmlContent, actionUrl, ref startSearchIndex);

                if (antiForgeryToken != null)
                {
                    return antiForgeryToken;
                }
            }

            return string.Empty;
        }

        private static string RetrieveAntiForgeryToken(string htmlContent, string actionLocation, ref int startIndex)
        {
            var formStartIndex = htmlContent.IndexOf("<form", startIndex, StringComparison.OrdinalIgnoreCase);
            var formEndIndex = htmlContent.IndexOf("</form>", startIndex, StringComparison.OrdinalIgnoreCase);

            if (formStartIndex == -1 || formEndIndex == -1)
            {
                //Unable to find the form start or end - finish the search
                startIndex = htmlContent.Length;
                return null;
            }

            formEndIndex = formEndIndex + "</form>".Length;
            startIndex = formEndIndex + 1;

            var htmlDocument = new XmlDocument();
            htmlDocument.LoadXml(htmlContent.Substring(formStartIndex, formEndIndex - formStartIndex));

            foreach (XmlAttribute attribute in htmlDocument.DocumentElement.Attributes)
            {
                if (string.Compare(attribute.Name, "action", true) == 0 && attribute.Value.EndsWith(actionLocation, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (XmlNode input in htmlDocument.GetElementsByTagName("input"))
                    {
                        if (input.Attributes["name"]?.Value == "__RequestVerificationToken" && input.Attributes["type"].Value == "hidden")
                        {
                            return input.Attributes["value"].Value;
                        }
                    }
                }
            }

            return null;
        }
    }
}
