// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
{
    internal static class StaticWebAssetsReader
    {
        private const string ManifestRootElementName = "StaticWebAssets";
        private const string VersionAttributeName = "Version";
        private const string ContentRootElementName = "ContentRoot";

        internal static IEnumerable<ContentRootMapping> Parse(Stream manifest)
        {
            var document = XDocument.Load(manifest);
            if (!string.Equals(document.Root.Name.LocalName, ManifestRootElementName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Invalid manifest format. Manifest root must be '{ManifestRootElementName}'");
            }

            var version = document.Root.Attribute(VersionAttributeName);
            if (version == null)
            {
                throw new InvalidOperationException($"Invalid manifest format. Manifest root element must contain a version '{VersionAttributeName}' attribute");
            }

            if (version.Value != "1.0")
            {
                throw new InvalidOperationException($"Unknown manifest version. Manifest version must be '1.0'");
            }

            foreach (var element in document.Root.Elements())
            {
                if (!string.Equals(element.Name.LocalName, ContentRootElementName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Invalid manifest format. Invalid element '{element.Name.LocalName}'. All {StaticWebAssetsLoader.StaticWebAssetsManifestName} child elements must be '{ContentRootElementName}' elements.");
                }
                if (!element.IsEmpty)
                {
                    throw new InvalidOperationException($"Invalid manifest format. {ContentRootElementName} can't have content.");
                }

                var basePath = ParseRequiredAttribute(element, "BasePath");
                var path = ParseRequiredAttribute(element, "Path");
                yield return new ContentRootMapping(basePath, path);
            }
        }

        private static string ParseRequiredAttribute(XElement element, string attributeName)
        {
            var attribute = element.Attribute(attributeName);
            if (attribute == null)
            {
                throw new InvalidOperationException($"Invalid manifest format. Missing {attributeName} attribute in '{ContentRootElementName}' element.");
            }
            return attribute.Value;
        }

        internal readonly struct ContentRootMapping
        {
            public ContentRootMapping(string basePath, string path)
            {
                BasePath = basePath;
                Path = path;
            }

            public string BasePath { get; }
            public string Path { get; }
        }
    }
}
