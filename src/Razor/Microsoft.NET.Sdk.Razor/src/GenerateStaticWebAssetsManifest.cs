// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class GenerateStaticWebAssetsManifest : Task
    {
        private const string ContentRoot = "ContentRoot";
        private const string BasePath = "BasePath";
        private const string SourceId = "SourceId";

        [Required]
        public string TargetManifestPath { get; set; }

        [Required]
        public ITaskItem[] ContentRootDefinitions { get; set; }

        public override bool Execute()
        {
            if (!ValidateArguments())
            {
                return false;
            }

            return ExecuteCore();
        }

        private bool ExecuteCore()
        {
            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            var root = new XElement(
                "StaticWebAssets",
                new XAttribute("Version", "1.0"),
                CreateNodes());

            document.Add(root);

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = true,
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = false,
                Async = true
            };

            using (var xmlWriter = GetXmlWriter(settings))
            {
                document.WriteTo(xmlWriter);
            }

            return !Log.HasLoggedErrors;
        }

        private IEnumerable<XElement> CreateNodes()
        {
            var nodes = new List<XElement>();
            for (var i = 0; i < ContentRootDefinitions.Length; i++)
            {
                var contentRootDefinition = ContentRootDefinitions[i];
                var basePath = contentRootDefinition.GetMetadata(BasePath);
                var contentRoot = contentRootDefinition.GetMetadata(ContentRoot);

                // basePath is meant to be a prefix for the files under contentRoot. MSbuild
                // normalizes '\' according to the OS, but this is going to be part of the url
                // so it needs to always be '/'.
                var normalizedBasePath = basePath.Replace("\\", "/");

                // contentRoot can have forward and trailing slashes and sometimes consecutive directory
                // separators. To be more flexible we will normalize the content root so that it contains a
                // single trailing separator.
                var normalizedContentRoot = $"{contentRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}{Path.DirectorySeparatorChar}";

                // At this point we already know that there are no elements with different base paths and same content roots
                // or viceversa. Here we simply skip additional items that have the same base path and same content root.
                if (!nodes.Exists(e => e.Attribute("BasePath").Value.Equals(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
                    e.Attribute("Path").Value.Equals(normalizedContentRoot, StringComparison.OrdinalIgnoreCase)))
                {
                    nodes.Add(new XElement("ContentRoot",
                        new XAttribute("BasePath", normalizedBasePath),
                        new XAttribute("Path", normalizedContentRoot)));
                }
            }

            // Its important that we order the nodes here to produce a manifest deterministically.
            return nodes.OrderBy(e=>e.Attribute(BasePath).Value);
        }

        private XmlWriter GetXmlWriter(XmlWriterSettings settings)
        {
            var fileStream = new FileStream(TargetManifestPath, FileMode.Create);
            return XmlWriter.Create(fileStream, settings);
        }

        private bool ValidateArguments()
        {
            for (var i = 0; i < ContentRootDefinitions.Length; i++)
            {
                var contentRootDefinition = ContentRootDefinitions[i];
                if (!EnsureRequiredMetadata(contentRootDefinition, BasePath) ||
                    !EnsureRequiredMetadata(contentRootDefinition, ContentRoot) ||
                    !EnsureRequiredMetadata(contentRootDefinition, SourceId))
                {
                    return false;
                }
            }

            // We want to validate that there are no different item groups that share either the same base path
            // but different content roots or that share the same content root but different base paths.
            // We pass in all the static web assets that we discovered to this task without making any distinction for
            // duplicates, so here we skip elements for which we are already tracking an element with the same
            // content root path and same base path.

            // Case-sensitivity depends on the underlying OS so we are not going to do anything to enforce it here.
            // Any two items that match base path and content root in a case-insensitive way won't produce an error.
            // Any other two items will produce an error even if there is only a casing difference between either the
            // base paths or the content roots.
            var basePaths = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
            var contentRootPaths = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < ContentRootDefinitions.Length; i++)
            {
                var contentRootDefinition = ContentRootDefinitions[i];
                var basePath = contentRootDefinition.GetMetadata(BasePath);
                var contentRoot = contentRootDefinition.GetMetadata(ContentRoot);
                var sourceId = contentRootDefinition.GetMetadata(SourceId);

                if (basePaths.TryGetValue(basePath, out var existingBasePath))
                {
                    var existingBasePathContentRoot = existingBasePath.GetMetadata(ContentRoot);
                    var existingSourceId = existingBasePath.GetMetadata(SourceId);
                    if (!string.Equals(contentRoot, existingBasePathContentRoot, StringComparison.OrdinalIgnoreCase) &&
                        // We want to check this case to allow for client-side blazor projects to have multiple different content
                        // root sources exposed under the same base path while still requiring unique base paths/content roots across
                        // project/package boundaries.
                        !string.Equals(sourceId, existingSourceId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Case:
                        // Item2: /_content/Library, project:/project/aspnetContent2
                        // Item1: /_content/Library, package:/package/aspnetContent1
                        Log.LogError($"Duplicate base paths '{basePath}' for content root paths '{contentRoot}' and '{existingBasePathContentRoot}'. " +
                            $"('{contentRootDefinition.ItemSpec}', '{existingBasePath.ItemSpec}')");
                        return false;
                    }

                    // It was a duplicate, so we skip it.
                    // Case:
                    // Item1: /_content/Library, project:/project/aspnetContent
                    // Item2: /_content/Library, project:/project/aspnetContent

                    // It was a separate content root exposed from the same project/package, so we skip it.
                    // Case:
                    // Item1: /_content/Library, project:/project/aspnetContent/bin/debug/netstandard2.1/dist
                    // Item2: /_content/Library, project:/project/wwwroot
                }
                else
                {
                    if (contentRootPaths.TryGetValue(contentRoot, out var existingContentRoot))
                    {
                        // Case:
                        // Item1: /_content/Library1, /package/aspnetContent
                        // Item2: /_content/Library2, /package/aspnetContent
                        Log.LogError($"Duplicate content root paths '{contentRoot}' for base paths '{basePath}' and '{existingContentRoot.GetMetadata(BasePath)}' " +
                            $"('{contentRootDefinition.ItemSpec}', '{existingContentRoot.ItemSpec}')");
                        return false;
                    }
                }

                if (!basePaths.ContainsKey(basePath))
                {
                    basePaths.Add(basePath, contentRootDefinition);
                }

                if (!contentRootPaths.ContainsKey(contentRoot))
                {
                    contentRootPaths.Add(contentRoot, contentRootDefinition);
                }
            }

            return true;
        }

        private bool EnsureRequiredMetadata(ITaskItem item, string metadataName)
        {
            var value = item.GetMetadata(metadataName);
            if (string.IsNullOrEmpty(value))
            {
                Log.LogError($"Missing required metadata '{metadataName}' for '{item.ItemSpec}'.");
                return false;
            }

            return true;
        }
    }
}