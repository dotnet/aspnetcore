// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DOTNET5_4
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Extracts summary and remarks XML documentation from an XML documentation file.
    /// </summary>
    public class XmlDocumentationProvider
    {
        private readonly IEnumerable<XElement> _members;

        /// <summary>
        /// Instantiates a new instance of the <see cref="XmlDocumentationProvider"/>.
        /// </summary>
        /// <param name="xmlFileLocation">Path to the XML documentation file to read.</param>
        public XmlDocumentationProvider(string xmlFileLocation)
        {
            // XML file processing is defined by: https://msdn.microsoft.com/en-us/library/fsbx0t7x.aspx
            var xmlDocumentation = XDocument.Load(xmlFileLocation);
            var documentationRootMembers = xmlDocumentation.Root.Element("members");
            _members = documentationRootMembers.Elements("member");
        }

        /// <summary>
        /// Retrieves the <c>&lt;summary&gt;</c> documentation for the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id to lookup.</param>
        /// <returns><c>&lt;summary&gt;</c> documentation for the given <paramref name="id"/>.</returns>
        public string GetSummary(string id)
        {
            var associatedMemeber = GetMember(id);
            var summaryElement = associatedMemeber?.Element("summary");

            if (summaryElement != null)
            {
                var summaryValue = GetElementValue(summaryElement);

                return summaryValue;
            }

            return null;
        }

        /// <summary>
        /// Retrieves the <c>&lt;remarks&gt;</c> documentation for the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id to lookup.</param>
        /// <returns><c>&lt;remarks&gt;</c> documentation for the given <paramref name="id"/>.</returns>
        public string GetRemarks(string id)
        {
            var associatedMemeber = GetMember(id);
            var remarksElement = associatedMemeber?.Element("remarks");

            if (remarksElement != null)
            {
                var remarksValue = GetElementValue(remarksElement);

                return remarksValue;
            }

            return null;
        }

        /// <summary>
        /// Generates the <see cref="string"/> identifier for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to get the identifier for.</param>
        /// <returns>The <see cref="string"/> identifier for the given <paramref name="type"/>.</returns>
        public static string GetId(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return $"T:{type.FullName}";
        }

        /// <summary>
        /// Generates the <see cref="string"/> identifier for the given <paramref name="propertyInfo"/>.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> to get the identifier for.</param>
        /// <returns>The <see cref="string"/> identifier for the given <paramref name="propertyInfo"/>.</returns>
        public static string GetId(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            var declaringTypeInfo = propertyInfo.DeclaringType;
            return $"P:{declaringTypeInfo.FullName}.{propertyInfo.Name}";
        }

        private XElement GetMember(string id)
        {
            var associatedMemeber = _members
                .FirstOrDefault(element =>
                    string.Equals(element.Attribute("name").Value, id, StringComparison.Ordinal));

            return associatedMemeber;
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
#endif