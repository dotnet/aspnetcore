// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class AddMetapackageReferences : Task
    {
        [Required]
        public string ReferencePackagePath { get; set; }

        [Required]
        public ITaskItem[] BuildArtifacts { get; set; }

        [Required]
        public ITaskItem[] PackageArtifacts { get; set; }

        [Required]
        public ITaskItem[] ExternalDependencies { get; set; }

        public override bool Execute()
        {
            // Parse input
            var metapackageArtifacts = PackageArtifacts.Where(p => p.GetMetadata("Metapackage") == "true");
            var externalArtifacts = ExternalDependencies.Where(p => p.GetMetadata("Metapackage") == "true");
            var buildArtifacts = BuildArtifacts.Select(ArtifactInfo.Parse)
                .OfType<ArtifactInfo.Package>()
                .Where(p => !p.IsSymbolsArtifact);

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(ReferencePackagePath);

            // Project
            var projectElement = xmlDoc.FirstChild;

            // Items
            var itemGroupElement = xmlDoc.CreateElement("ItemGroup");
            Log.LogMessage(MessageImportance.High, $"Metapackage will include the following packages");

            foreach (var package in metapackageArtifacts)
            {
                var packageName = package.ItemSpec;
                var packageVersion = buildArtifacts
                    .Single(p => string.Equals(p.PackageInfo.Id, packageName, StringComparison.OrdinalIgnoreCase))
                    .PackageInfo.Version.ToString();
                Log.LogMessage(MessageImportance.High, $" - Package: {packageName} Version: [{packageVersion}]");

                var packageReferenceElement = xmlDoc.CreateElement("PackageReference");
                packageReferenceElement.SetAttribute("Include", packageName);
                packageReferenceElement.SetAttribute("Version", $"[{packageVersion}]");
                packageReferenceElement.SetAttribute("PrivateAssets", "None");

                itemGroupElement.AppendChild(packageReferenceElement);
            }

            foreach (var package in externalArtifacts)
            {
                var packageName = package.ItemSpec;
                var packageVersion = package.GetMetadata("Version");
                Log.LogMessage(MessageImportance.High, $" - Package: {packageName} Version: [{packageVersion}]");

                var packageReferenceElement = xmlDoc.CreateElement("PackageReference");
                packageReferenceElement.SetAttribute("Include", packageName);
                packageReferenceElement.SetAttribute("Version", $"[{packageVersion}]");
                packageReferenceElement.SetAttribute("PrivateAssets", "None");

                itemGroupElement.AppendChild(packageReferenceElement);
            }

            projectElement.AppendChild(itemGroupElement);

            // Save updated file
            xmlDoc.AppendChild(projectElement);
            xmlDoc.Save(ReferencePackagePath);

            return true;
        }
    }
}
