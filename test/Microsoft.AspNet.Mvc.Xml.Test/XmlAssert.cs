// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Xunit.Sdk;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// Xunit assertions related to Xml content.
    /// </summary>
    public static class XmlAssert
    {
        /// <summary>
        /// Compares two xml strings ignoring an element's attribute order.
        /// </summary>
        /// <param name="expectedXml">Expected xml string.</param>
        /// <param name="actualXml">Actual xml string.</param>
        public static void Equal(string expectedXml, string actualXml)
        {
            var sortedExpectedXDoc = SortAttributes(XDocument.Parse(expectedXml));
            var sortedActualXDoc = SortAttributes(XDocument.Parse(actualXml));

            bool areEqual = XNode.DeepEquals(sortedExpectedXDoc, sortedActualXDoc);

            if (!areEqual)
            {
                throw new EqualException(sortedExpectedXDoc, sortedActualXDoc);
            }
        }

        private static XDocument SortAttributes(XDocument doc)
        {
            return new XDocument(
                    doc.Declaration,
                    SortAttributes(doc.Root));
        }

        private static XElement SortAttributes(XElement element)
        {
            return new XElement(
                    element.Name,
                    element.Attributes().OrderBy(a => a.Name.ToString()),
                    element.Elements().Select(child => SortAttributes(child)));
        }
    }
}