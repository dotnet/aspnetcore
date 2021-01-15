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
        private const string NodePath = "Path";
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

                // Here we simply skip additional items that have the same base path and same content root.
                if (!nodes.Exists(e => e.Attribute("BasePath").Value.Equals(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
                    e.Attribute("Path").Value.Equals(normalizedContentRoot, StringComparison.OrdinalIgnoreCase)))
                {
                    nodes.Add(new XElement("ContentRoot",
                        new XAttribute("BasePath", normalizedBasePath),
                        new XAttribute("Path", normalizedContentRoot)));
                }
            }

            // Its important that we order the nodes here to produce a manifest deterministically.
            return nodes.OrderBy(e=>e.Attribute(BasePath).Value).ThenBy(e => e.Attribute(NodePath).Value);
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
