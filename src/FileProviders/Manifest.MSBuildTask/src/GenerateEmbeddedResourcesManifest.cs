// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest.Task
{
    /// <summary>
    /// Task for generating a manifest file out of the embedded resources in an
    /// assembly.
    /// </summary>
    public class GenerateEmbeddedResourcesManifest : Microsoft.Build.Utilities.Task
    {
        private const string LogicalName = "LogicalName";
        private const string ManifestResourceName = "ManifestResourceName";
        private const string TargetPath = "TargetPath";

        [Required]
        public ITaskItem[] EmbeddedFiles { get; set; }

        [Required]
        public string ManifestFile { get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            var processedItems = CreateEmbeddedItems(EmbeddedFiles);

            var manifest = BuildManifest(processedItems);

            var document = manifest.ToXmlDocument();

            var settings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                CloseOutput = true
            };

            using (var xmlWriter = GetXmlWriter(settings))
            {
                document.WriteTo(xmlWriter);
            }

            return true;
        }

        protected virtual XmlWriter GetXmlWriter(XmlWriterSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var fileStream = new FileStream(ManifestFile, FileMode.Create);
            return XmlWriter.Create(fileStream, settings);
        }

        public EmbeddedItem[] CreateEmbeddedItems(params ITaskItem[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            return items.Select(er => new EmbeddedItem
            {
                ManifestFilePath = GetManifestPath(er),
                AssemblyResourceName = GetAssemblyResourceName(er)
            }).ToArray();
        }

        public Manifest BuildManifest(EmbeddedItem[] processedItems)
        {
            if (processedItems == null)
            {
                throw new ArgumentNullException(nameof(processedItems));
            }

            var manifest = new Manifest();
            foreach (var item in processedItems)
            {
                manifest.AddElement(item.ManifestFilePath, item.AssemblyResourceName);
            }

            return manifest;
        }

        private string GetManifestPath(ITaskItem taskItem) => string.Equals(taskItem.GetMetadata(LogicalName), taskItem.GetMetadata(ManifestResourceName)) ?
            taskItem.GetMetadata(TargetPath) :
            NormalizePath(taskItem.GetMetadata(LogicalName));

        private string GetAssemblyResourceName(ITaskItem taskItem) => string.Equals(taskItem.GetMetadata(LogicalName), taskItem.GetMetadata(ManifestResourceName)) ?
            taskItem.GetMetadata(ManifestResourceName) :
            taskItem.GetMetadata(LogicalName);

        private string NormalizePath(string path) => Path.DirectorySeparatorChar == '\\' ?
            path.Replace("/", "\\") : path.Replace("\\", "/");
    }
}
