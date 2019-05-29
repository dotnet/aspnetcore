// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore
{
    internal static class StaticWebAssetsHelper
    {
        private const string StaticWebAssetsManifestName = "Microsoft.AspNetCore.StaticWebAssets.xml";
        private const string ManifestRootElementName = "StaticWebAssets";
        private const string VersionAttributeName = "Version";
        private const string ContentRootElementName = "ContentRoot";

        internal static void UseStaticWebAssets(this IWebHostEnvironment environment)
        {
            using (var manifest = ResolveManifest(environment))
            {
                if (manifest != null)
                {
                    UseStaticWebAssetsCore(environment, manifest);
                }
            }
        }

        internal static void UseStaticWebAssetsCore(this IWebHostEnvironment environment, Stream manifest)
        {
            var staticWebAssetsFileProvider = new List<IFileProvider>();
            var webRootFileProvider = environment.WebRootFileProvider;

            var staticWebAssetContentRoots = Parse(manifest)
                .Select(cr => new StaticWebAssetsFileProvider(cr.BasePath, cr.Path))
                .OfType<IFileProvider>() // Upcast so we can insert on the resulting list.
                .ToList();

            if (staticWebAssetContentRoots.Count == 0)
            {
                return;
            }
            else
            {
                staticWebAssetContentRoots.Insert(0, webRootFileProvider);
                environment.WebRootFileProvider = new CompositeFileProvider(staticWebAssetContentRoots);
            }
        }

        internal static Stream ResolveManifest(IWebHostEnvironment environment)
        {
            var assembly = Assembly.Load(environment.ApplicationName);
            if (assembly.GetManifestResourceNames().Any(a => a == StaticWebAssetsManifestName))
            {
                return assembly.GetManifestResourceStream(StaticWebAssetsManifestName);
            }
            else
            {
                // Fallback to physical file as we plan to use a file on disk instead of the embedded resource.
                var filePath = Path.Combine(Path.GetDirectoryName(new Uri(assembly.CodeBase).LocalPath), $"{environment.ApplicationName}.StaticWebAssets.xml");
                if (File.Exists(filePath))
                {
                    return File.OpenRead(filePath);
                }
                else
                {
                    // A missing manifest might simply mean that the feature is not enabled, so we simply
                    // return early. Misconfigurations will be uncommon given that the entire process is automated
                    // at build time.
                    return null;
                }
            }
        }

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
                    throw new InvalidOperationException($"Invalid manifest format. Invalid element '{element.Name.LocalName}'. All {StaticWebAssetsManifestName} child elements must be '{ContentRootElementName}' elements.");
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

        internal class ContentRootMapping
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
