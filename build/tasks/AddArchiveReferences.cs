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
    public class AddArchiveReferences : Task
    {
        [Required]
        public string ReferencePackagePath { get; set; }

        [Required]
        public bool RemoveTimestamp { get; set; }

        [Required]
        public ITaskItem[] BuildArtifacts { get; set; }

        [Required]
        public ITaskItem[] PackageArtifacts { get; set; }

        [Required]
        public ITaskItem[] ExternalDependencies { get; set; }

        public override bool Execute()
        {
            // Parse input
            var externalArchiveArtifacts = ExternalDependencies.Where(p => p.GetMetadata("LZMA") == "true");
            var externalArchiveTools = ExternalDependencies.Where(p => p.GetMetadata("LZMATools") == "true");
            var archiveArtifacts = PackageArtifacts.Where(p => p.GetMetadata("LZMA") == "true");
            var archiveTools = PackageArtifacts.Where(p => p.GetMetadata("LZMATools") == "true");
            var buildArtifacts = BuildArtifacts.Select(ArtifactInfo.Parse)
                .OfType<ArtifactInfo.Package>()
                .Where(p => !p.IsSymbolsArtifact);

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(ReferencePackagePath);

            // Project
            var projectElement = xmlDoc.FirstChild;

            // Items
            var itemGroupElement = xmlDoc.CreateElement("ItemGroup");
            Log.LogMessage(MessageImportance.High, $"Archive will include the following packages");

            foreach (var package in archiveArtifacts)
            {
                var packageName = package.ItemSpec;
                var packageVersion = buildArtifacts
                    .Single(p => string.Equals(p.PackageInfo.Id, packageName, StringComparison.OrdinalIgnoreCase))
                    .PackageInfo.Version.ToString();

                if (string.Equals(RemoveTimestamp, "true", StringComparison.OrdinalIgnoreCase))
                {
                    var version = new NuGetVersion(packageVersion);
                    var updatedVersion = new NuGetVersion(version.Version, VersionUtilities.GetNoTimestampReleaseLabel(version.Release));
                    packageVersion = updatedVersion.ToNormalizedString();
                }

                Log.LogMessage(MessageImportance.High, $" - Package: {packageName} Version: {packageVersion}");

                var packageReferenceElement = xmlDoc.CreateElement("PackageReference");
                packageReferenceElement.SetAttribute("Include", packageName);
                packageReferenceElement.SetAttribute("Version", packageVersion);

                itemGroupElement.AppendChild(packageReferenceElement);
            }

            foreach (var package in externalArchiveArtifacts)
            {
                var packageName = package.ItemSpec;
                var packageVersion = package.GetMetadata("Version");
                Log.LogMessage(MessageImportance.High, $" - Package: {packageName} Version: {packageVersion}");

                var packageReferenceElement = xmlDoc.CreateElement("PackageReference");
                packageReferenceElement.SetAttribute("Include", packageName);
                packageReferenceElement.SetAttribute("Version", packageVersion);

                itemGroupElement.AppendChild(packageReferenceElement);
            }

            foreach (var package in archiveTools)
            {
                var packageName = package.ItemSpec;
                var packageVersion = buildArtifacts
                    .Single(p => string.Equals(p.PackageInfo.Id, packageName, StringComparison.OrdinalIgnoreCase))
                    .PackageInfo.Version.ToString();

                if (string.Equals(RemoveTimestamp, "true", StringComparison.OrdinalIgnoreCase))
                {
                    var version = new NuGetVersion(packageVersion);
                    var updatedVersion = new NuGetVersion(version.Version, VersionUtilities.GetNoTimestampReleaseLabel(version.Release));
                    packageVersion = updatedVersion.ToNormalizedString();
                }

                Log.LogMessage(MessageImportance.High, $" - Tool: {packageName} Version: {packageVersion}");

                var packageReferenceElement = xmlDoc.CreateElement("DotNetCliToolReference");
                packageReferenceElement.SetAttribute("Include", packageName);
                packageReferenceElement.SetAttribute("Version", packageVersion);

                itemGroupElement.AppendChild(packageReferenceElement);
            }

            foreach (var package in externalArchiveTools)
            {
                var packageName = package.ItemSpec;
                var packageVersion = package.GetMetadata("Version");
                Log.LogMessage(MessageImportance.High, $" - Tool: {packageName} Version: {packageVersion}");

                var packageReferenceElement = xmlDoc.CreateElement("DotNetCliToolReference");
                packageReferenceElement.SetAttribute("Include", packageName);
                packageReferenceElement.SetAttribute("Version", packageVersion);

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
