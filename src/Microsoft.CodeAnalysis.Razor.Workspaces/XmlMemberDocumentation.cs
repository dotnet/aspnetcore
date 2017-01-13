// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Razor
{
    /// <summary>
    /// Extracts summary and remarks XML documentation from XML member documentation.
    /// </summary>
    internal class XmlMemberDocumentation
    {
        private readonly XElement _element;

        public XmlMemberDocumentation(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException(Resources.FormatArgument_Cannot_Be_Null_Or_Empty(nameof(content)));
            }

            // the structure of the XML is defined by: https://msdn.microsoft.com/en-us/library/fsbx0t7x.aspx
            // we expect the root node of the content we are passed to always be 'member'.
            _element = XElement.Parse(content);
            Debug.Assert(_element.Name == "member");
        }

        /// <summary>
        /// Retrieves the <c>&lt;summary&gt;</c> documentation.
        /// </summary>
        /// <returns><c>&lt;summary&gt;</c> documentation.</returns>
        public string GetSummary()
        {
            var summaryElement = _element.Element("summary");
            if (summaryElement != null)
            {
                var summaryValue = GetElementValue(summaryElement);

                return summaryValue;
            }

            return null;
        }

        /// <summary>
        /// Retrieves the <c>&lt;remarks&gt;</c> documentation.
        /// </summary>
        /// <returns><c>&lt;remarks&gt;</c> documentation.</returns>
        public string GetRemarks()
        {
            var remarksElement = _element.Element("remarks");

            if (remarksElement != null)
            {
                var remarksValue = GetElementValue(remarksElement);

                return remarksValue;
            }

            return null;
        }

        private static string GetElementValue(XElement element)
        {
            var stringBuilder = new StringBuilder();
            var node = element.FirstNode;

            while (node != null)
            {
                stringBuilder.Append(node.ToString(SaveOptions.DisableFormatting));

                node = node.NextNode;
            }

            return stringBuilder.ToString().Trim();
        }
    }
}