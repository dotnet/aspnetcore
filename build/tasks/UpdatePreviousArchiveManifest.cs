// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Versioning;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class UpdatePreviousArchiveManifest : Task
    {
        [Required]
        public string OutputPath { get; set; }

        [Required]
        public ITaskItem[] Contents { get; set; }

        public override bool Execute()
        {
            var xmlDoc = new XmlDocument();

            // Project
            var projectElement = xmlDoc.CreateElement("Project");

            // Items
            var itemGroupElement = xmlDoc.CreateElement("ItemGroup");

            foreach (var content in Contents)
            {
                var contentElement = xmlDoc.CreateElement("PreviousLzmaContents");
                contentElement.SetAttribute("Include", $"{content.GetRecursiveDir()}{content.GetFileName()}{content.GetExtension()}");
                itemGroupElement.AppendChild(contentElement);
                // Recursive will be lost during round tripping using a props file. To fix this, set the RecursiveDir to RelativeDir.
                // This can only be done in a task as MSBuild prevents overwritting reserved metadata.
            }

            projectElement.AppendChild(itemGroupElement);

            // Save updated file
            xmlDoc.AppendChild(projectElement);
            xmlDoc.Save(OutputPath);

            return true;
        }
    }
}
