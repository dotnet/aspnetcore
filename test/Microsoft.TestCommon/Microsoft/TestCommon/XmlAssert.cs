// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// Assert class that compares two XML strings for equality. Namespaces are ignored during comparison
    /// </summary>
    public class XmlAssert
    {
        public void Equal(string expected, string actual, params RegexReplacement[] regexReplacements)
        {
            if (regexReplacements != null)
            {
                for (int i = 0; i < regexReplacements.Length; i++)
                {
                    actual = regexReplacements[i].Regex.Replace(actual, regexReplacements[i].Replacement);
                }
            }

            Equal(XElement.Parse(expected), XElement.Parse(actual));
        }

        public void Equal(XElement expected, XElement actual)
        {
            Assert.Equal(Normalize(expected).ToString(), Normalize(actual).ToString());
        }

        private static XElement Normalize(XElement element)
        {
            if (element.HasElements)
            {
                return new XElement(
                    Encode(element.Name),
                    Normalize(element.Attributes()),
                    Normalize(element.Elements()));
            }

            if (element.IsEmpty)
            {
                return new XElement(
                    Encode(element.Name),
                    Normalize(element.Attributes()));
            }
            else
            {
                return new XElement(
                    Encode(element.Name),
                    Normalize(element.Attributes()),
                    element.Value);
            }
        }

        private static IEnumerable<XAttribute> Normalize(IEnumerable<XAttribute> attributes)
        {
            return attributes
                    .Where((attrib) => !attrib.IsNamespaceDeclaration)
                    .Select((attrib) => new XAttribute(Encode(attrib.Name), attrib.Value))
                    .OrderBy(a => a.Name.ToString());
        }

        private static IEnumerable<XElement> Normalize(IEnumerable<XElement> elements)
        {
            return elements
                    .Select(e => Normalize(e))
                    .OrderBy(a => a.ToString());
        }

        private static string Encode(XName name)
        {
            return string.Format("{0}_{1}", HttpUtility.UrlEncode(name.NamespaceName).Replace('%', '_'), name.LocalName);
        }
    }
}