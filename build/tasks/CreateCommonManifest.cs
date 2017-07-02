// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    /// <summary>
    /// Creates a common manifest file used for trimming publish output given a list of packages and package definitions containing their versions.
    /// </summary>
    public class CreateCommonManifest : Task
    {
        /// <summary>
        /// The path for the common manifest file to be created.
        /// </summary>
        /// <returns></returns>
        [Required]
        public string DestinationFilePath { get; set; }

        /// <summary>
        /// The packages to include in the common manifest file.
        /// </summary>
        /// <returns></returns>
        [Required]
        public ITaskItem[] Packages { get; set; }

        /// <summary>
        /// The package definitions used for resolving package versions.
        /// </summary>
        /// <returns></returns>
        [Required]
        public ITaskItem[] PackageDefinitions { get; set; }

        public override bool Execute()
        {
            var xmlDoc = new XmlDocument();
            var packagesElement = xmlDoc.CreateElement("StoreArtifacts");

            foreach (var package in Packages)
            {
                var packageName = package.ItemSpec;
                var packageElement = xmlDoc.CreateElement("Package");

                var idAttribute = xmlDoc.CreateAttribute("Id");
                idAttribute.Value = packageName;
                packageElement.Attributes.Append(idAttribute);

                var versionAttribute = xmlDoc.CreateAttribute("Version");
                versionAttribute.Value = PackageDefinitions
                    .Where(p => string.Equals(p.GetMetadata("Name"), packageName, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.GetMetadata("Version")).Single();
                packageElement.Attributes.Append(versionAttribute);

                packagesElement.AppendChild(packageElement);
            }

            xmlDoc.AppendChild(packagesElement);
            xmlDoc.Save(DestinationFilePath);

            return true;
        }
    }
}
