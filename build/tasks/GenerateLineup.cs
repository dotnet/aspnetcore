// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RepoTasks.ProjectModel;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class GenerateLineup : Task
    {
        [Required]
        public ITaskItem[] Artifacts { get; set; }

        [Required]
        public string OutputPath { get; set; }

        // Can be set to filter the lists of packages when produce a list for a specific repository
        public string Repository { get; set; }

        // Items to add to the RestoreAdditionalProjectSources list in project
        public ITaskItem[] RestoreAdditionalSources { get; set; }

        public bool UseFloatingVersions { get; set; }

        public string BuildNumber { get; set; }

        public override bool Execute()
        {
            OutputPath = OutputPath.Replace('\\', '/');
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            if (UseFloatingVersions && string.IsNullOrEmpty(BuildNumber))
            {
                Log.LogWarning("Cannot compute floating versions when BuildNumber is not specified");
            }

            var items = new XElement("ItemGroup");
            var props = new XElement("PropertyGroup");
            var root = new XElement("Project", props, items);
            var doc = new XDocument(root);

            if (RestoreAdditionalSources?.Length > 0)
            {
                var sources = RestoreAdditionalSources.Aggregate("$(RestoreAdditionalProjectSources)", (sum, piece) => sum + ";" + piece.ItemSpec);
                props.Add(new XElement("RestoreAdditionalProjectSources", sources));
            }

            var packages = new List<PackageInfo>();

            foreach (var pkg in Artifacts.Select(ArtifactInfo.Parse)
                .OfType<ArtifactInfo.Package>()
                .Where(p => !p.IsSymbolsArtifact
                        && (string.IsNullOrEmpty(Repository)
                        || !Repository.Equals(p.RepoName, StringComparison.OrdinalIgnoreCase))))
            {
                packages.Add(pkg.PackageInfo);
            }

            foreach (var pkg in packages.OrderBy(i => i.Id))
            {
                var version = pkg.Version.ToString();
                if (UseFloatingVersions && version.EndsWith(BuildNumber))
                {
                    version = version.Substring(0, version.Length - BuildNumber.Length) + "*";
                }

                var refType = "DotNetCliTool".Equals(pkg.PackageType, StringComparison.OrdinalIgnoreCase)
                    ? "DotNetCliToolReference"
                    : "PackageReference";

                items.Add(new XElement(refType,
                    new XAttribute("Update", pkg.Id),
                    new XAttribute("Version", version)));
            }

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
            };
            using (var writer = XmlWriter.Create(OutputPath, settings))
            {
                Log.LogMessage(MessageImportance.High, $"Generate {OutputPath}");
                doc.Save(writer);
            }
            return true;
        }
    }
}
