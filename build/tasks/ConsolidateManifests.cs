// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    // Takes multiple runtime store manifests and create a consolidated manifest containing all unique entries.
    public class ConsolidateManifests : Task
    {
        [Required]
        public ITaskItem[] Manifests { get; set; }

        [Required]
        public string ManifestDestination { get; set; }

        public override bool Execute()
        {
            var artifacts = new HashSet<Tuple<string, string>>();

            // Construct database of all artifacts
            foreach (var manifest in Manifests)
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(manifest.GetMetadata("FullPath"));
                var storeArtifacts = xmlDoc.SelectSingleNode("/StoreArtifacts");

                foreach (XmlNode artifact in storeArtifacts.ChildNodes)
                {
                    artifacts.Add(new Tuple<string, string>(artifact.Attributes["Id"].Value, artifact.Attributes["Version"].Value));
                }
            }


            var consolidatedXmlDoc = new XmlDocument();
            var packagesElement = consolidatedXmlDoc.CreateElement("StoreArtifacts");

            foreach (var artifact in artifacts)
            {
                var packageElement = consolidatedXmlDoc.CreateElement("Package");
                packageElement.SetAttribute("Id", artifact.Item1);
                packageElement.SetAttribute("Version", artifact.Item2);

                packagesElement.AppendChild(packageElement);
            }

            consolidatedXmlDoc.AppendChild(packagesElement);
            consolidatedXmlDoc.Save(ManifestDestination);

            return true;
        }
    }
}
