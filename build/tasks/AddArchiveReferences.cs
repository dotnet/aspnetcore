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
        public string MetaPackageVersion { get; set; }

        [Required]
        public ITaskItem[] BuildArtifacts { get; set; }

        [Required]
        public ITaskItem[] PackageArtifacts { get; set; }

        [Required]
        public ITaskItem[] ExternalDependencies { get; set; }

        public override bool Execute()
        {
            // Parse input
            var externalArchiveArtifacts = ExternalDependencies.Where(p => p.GetMetadata("LZMA") == "true" && p.GetMetadata("PackageType") == "Dependency");
            var externalArchiveTools = ExternalDependencies.Where(p => p.GetMetadata("LZMA") == "true" && p.GetMetadata("PackageType") == "DotnetCliTool");
            var archiveArtifacts = PackageArtifacts.Where(p => p.GetMetadata("LZMA") == "true" && p.GetMetadata("PackageType") == "Dependency");
            var archiveTools = PackageArtifacts.Where(p => p.GetMetadata("LZMA") == "true" && p.GetMetadata("PackageType") == "DotnetCliTool");
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

                string packageVersion;

                if(string.Equals(packageName, "Microsoft.AspNetCore.All", StringComparison.OrdinalIgnoreCase))
                {
                    packageVersion = MetaPackageVersion;
                }
                else
                {
                    try
                    {
                        packageVersion = buildArtifacts
                            .Single(p => string.Equals(p.PackageInfo.Id, packageName, StringComparison.OrdinalIgnoreCase))
                            .PackageInfo.Version.ToString();
                    }
                    catch (InvalidOperationException)
                    {
                        Log.LogError($"Missing Package: {packageName} from artifacts archive.");
                        throw;
                    }
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

                string packageVersion;

                try{
                    packageVersion = buildArtifacts
                        .Single(p => string.Equals(p.PackageInfo.Id, packageName, StringComparison.OrdinalIgnoreCase))
                        .PackageInfo.Version.ToString();
                }
                catch(InvalidOperationException)
                {
                    Log.LogError($"Missing Package: {packageName} from tools archive.");
                    throw;
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
