// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50 // Cannot accurately resolve the location of the documentation XML file in coreclr.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Factory for providing <see cref="TagHelperUsageDescriptor"/>s from <see cref="Type"/>s and
    /// <see cref="PropertyInfo"/>s.
    /// </summary>
    public static class TagHelperUsageDescriptorFactory
    {
        /// <summary>
        /// Creates a <see cref="TagHelperUsageDescriptor"/> from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to create a <see cref="TagHelperUsageDescriptor"/> from.</param>
        /// <returns>A <see cref="TagHelperUsageDescriptor"/> that describes the summary and remarks XML documentation
        /// for the given <paramref name="type"/>.</returns>
        public static TagHelperUsageDescriptor CreateDescriptor([NotNull] Type type)
        {
            var id = XmlDocumentationProvider.GetId(type);

            return CreateDescriptorCore(type.Assembly, id);
        }

        /// <summary>
        /// Creates a <see cref="TagHelperUsageDescriptor"/> from the given <paramref name="propertyInfo"/>.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> to create a
        /// <see cref="TagHelperUsageDescriptor"/> from.</param>
        /// <returns>A <see cref="TagHelperUsageDescriptor"/> that describes the summary and remarks XML documentation
        /// for the given <paramref name="propertyInfo"/>.</returns>
        public static TagHelperUsageDescriptor CreateDescriptor([NotNull] PropertyInfo propertyInfo)
        {
            var id = XmlDocumentationProvider.GetId(propertyInfo);
            var declaringAssembly = propertyInfo.DeclaringType.Assembly;

            return CreateDescriptorCore(declaringAssembly, id);
        }

        private static TagHelperUsageDescriptor CreateDescriptorCore(Assembly assembly, string id)
        {
            var assemblyLocation = assembly.Location;

            if (string.IsNullOrEmpty(assemblyLocation) && !string.IsNullOrEmpty(assembly.CodeBase))
            {
                var uri = new UriBuilder(assembly.CodeBase);

                // Normalize the path to a UNC path. This will remove things like file:// from start of the uri.Path.
                assemblyLocation = Uri.UnescapeDataString(uri.Path);
            }

            // Couldn't resolve a valid assemblyLocation.
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                return null;
            }

            var xmlDocumentationFile = GetXmlDocumentationFile(assembly, assemblyLocation);

            // Only want to process the file if it exists.
            if (xmlDocumentationFile != null)
            {
                var documentationProvider = new XmlDocumentationProvider(xmlDocumentationFile.FullName);

                var summary = documentationProvider.GetSummary(id);
                var remarks = documentationProvider.GetRemarks(id);

                if (!string.IsNullOrEmpty(summary) || !string.IsNullOrEmpty(remarks))
                {
                    return new TagHelperUsageDescriptor(summary, remarks);
                }
            }

            return null;
        }

        private static FileInfo GetXmlDocumentationFile(Assembly assembly, string assemblyLocation)
        {
            try
            {
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                var assemblyName = Path.GetFileName(assemblyLocation);
                var assemblyXmlDocumentationName = Path.ChangeExtension(assemblyName, ".xml");

                // Check for a localized XML file for the current culture.
                var xmlDocumentationFile = GetLocalizedXmlDocumentationFile(
                    CultureInfo.CurrentCulture,
                    assemblyDirectory,
                    assemblyXmlDocumentationName);

                if (xmlDocumentationFile == null)
                {
                    // Check for a culture-neutral XML file next to the assembly
                    xmlDocumentationFile = new FileInfo(
                        Path.Combine(assemblyDirectory, assemblyXmlDocumentationName));

                    if (!xmlDocumentationFile.Exists)
                    {
                        xmlDocumentationFile = null;
                    }
                }

                return xmlDocumentationFile;
            }
            catch (ArgumentException)
            {
                // Could not resolve XML file.
                return null;
            }
        }

        private static IEnumerable<string> ExpandPaths(
            CultureInfo culture,
            string assemblyDirectory,
            string assemblyXmlDocumentationName)
        {
            // Following the fall-back process defined by:
            // https://msdn.microsoft.com/en-us/library/sb6a8618.aspx#cpconpackagingdeployingresourcesanchor1
            do
            {
                var cultureName = culture.Name;
                var cultureSpecificFileName =
                    Path.ChangeExtension(assemblyXmlDocumentationName, cultureName + ".xml");

                // Look for a culture specific XML file next to the assembly.
                yield return Path.Combine(assemblyDirectory, cultureSpecificFileName);

                // Look for an XML file with the same name as the assembly in a culture specific directory.
                yield return Path.Combine(assemblyDirectory, cultureName, assemblyXmlDocumentationName);

                // Look for a culture specific XML file in a culture specific directory.
                yield return Path.Combine(assemblyDirectory, cultureName, cultureSpecificFileName);

                culture = culture.Parent;
            } while (culture != null && culture != CultureInfo.InvariantCulture);
        }

        private static FileInfo GetLocalizedXmlDocumentationFile(
            CultureInfo culture,
            string assemblyDirectory,
            string assemblyXmlDocumentationName)
        {
            var localizedXmlPaths = ExpandPaths(culture, assemblyDirectory, assemblyXmlDocumentationName);
            var xmlDocumentationFile = localizedXmlPaths
                .Select(path => new FileInfo(path))
                .FirstOrDefault(file => file.Exists);

            return xmlDocumentationFile;
        }
    }
}
#endif