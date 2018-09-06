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
    public class AddMetapackageReferences : Task
    {
        [Required]
        public string ReferencePackagePath { get; set; }

        [Required]
        public string MetapackageReferenceType { get; set; }

        [Required]
        public string DependencyVersionRangeType { get; set; }

        // MSBuild doesn't allow binding to enums directly.
        private enum VersionRangeType
        {
            Minimum, // [1.1.1, )
            MajorMinor, // [1.1.1, 1.2.0)
        }

        [Required]
        public ITaskItem[] PackageArtifacts { get; set; }

        [Required]
        public ITaskItem[] ExternalDependencies { get; set; }

        public override bool Execute()
        {
            if (!Enum.TryParse<VersionRangeType>(DependencyVersionRangeType, out var dependencyVersionType))
            {
                Log.LogError("Unexpected value {0} for DependencyVersionRangeType", DependencyVersionRangeType);
                return false;
            }

            // Parse input
            var metapackageArtifacts = PackageArtifacts.Where(p => p.GetMetadata(MetapackageReferenceType) == "true");
            var externalArtifacts = ExternalDependencies.Where(p => p.GetMetadata(MetapackageReferenceType) == "true");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(ReferencePackagePath);

            // Project
            var projectElement = xmlDoc.FirstChild;

            // Items
            var itemGroupElement = xmlDoc.CreateElement("ItemGroup");
            Log.LogMessage(MessageImportance.High, $"{MetapackageReferenceType} will include the following packages");

            foreach (var package in metapackageArtifacts)
            {
                var packageName = package.ItemSpec;
                var packageVersion = package.GetMetadata("Version");
                if (string.IsNullOrEmpty(packageVersion))
                {
                    Log.LogError("Missing version information for package {0}", packageName);
                    continue;
                }

                var packageVersionValue = GetDependencyVersion(dependencyVersionType, packageName, packageVersion);
                Log.LogMessage(MessageImportance.High, $" - Package: {packageName} Version: {packageVersionValue}");

                var packageReferenceElement = xmlDoc.CreateElement("PackageReference");
                packageReferenceElement.SetAttribute("Include", packageName);
                packageReferenceElement.SetAttribute("Version", packageVersionValue);
                packageReferenceElement.SetAttribute("PrivateAssets", "None");

                itemGroupElement.AppendChild(packageReferenceElement);
            }

            foreach (var package in externalArtifacts)
            {
                var packageName = package.ItemSpec;
                var packageVersion = package.GetMetadata("Version");

                if (string.IsNullOrEmpty(packageVersion))
                {
                    Log.LogError("Missing version information for package {0}", packageName);
                    continue;
                }

                var packageVersionValue =
                    Enum.TryParse<VersionRangeType>(package.GetMetadata("MetapackageVersionRangeType"), out var packageVersionType)
                    ? GetDependencyVersion(packageVersionType, packageName, packageVersion)
                    : GetDependencyVersion(dependencyVersionType, packageName, packageVersion);

                Log.LogMessage(MessageImportance.High, $" - Package: {packageName} Version: {packageVersionValue}");

                var packageReferenceElement = xmlDoc.CreateElement("PackageReference");
                packageReferenceElement.SetAttribute("Include", packageName);
                packageReferenceElement.SetAttribute("Version", packageVersionValue);
                packageReferenceElement.SetAttribute("PrivateAssets", "None");

                itemGroupElement.AppendChild(packageReferenceElement);
            }

            projectElement.AppendChild(itemGroupElement);

            // Save updated file
            xmlDoc.AppendChild(projectElement);
            xmlDoc.Save(ReferencePackagePath);

            return !Log.HasLoggedErrors;
        }

        private string GetDependencyVersion(VersionRangeType dependencyVersionType, string packageName, string packageVersion)
        {
            switch (dependencyVersionType)
            {
                case VersionRangeType.MajorMinor:
                    if (!NuGetVersion.TryParse(packageVersion, out var nugetVersion))
                    {
                        Log.LogError("Invalid NuGet version '{0}' for package {1}", packageVersion, packageName);
                        return null;
                    }
                    return $"[{packageVersion}, {nugetVersion.Major}.{nugetVersion.Minor + 1}.0)";
                case VersionRangeType.Minimum:
                    return packageVersion;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
