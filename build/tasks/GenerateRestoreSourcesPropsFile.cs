// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class GenerateRestoreSourcesPropsFile : Task
    {
        [Required]
        public ITaskItem[] Sources { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            OutputPath = OutputPath.Replace('\\', '/');
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var sources = new XElement("DotNetRestoreSources");
            var propertyGroup = new XElement("PropertyGroup", sources);
            var doc = new XDocument(new XElement("Project", propertyGroup));

            propertyGroup.Add(new XElement("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)"));

            var sb = new StringBuilder();

            foreach (var source in Sources)
            {
                sb.Append(source.ItemSpec).AppendLine(";");
            }

            sources.SetValue(sb.ToString());

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
            };
            using (var writer = XmlWriter.Create(OutputPath, settings))
            {
                Log.LogMessage(MessageImportance.Normal, $"Generate {OutputPath}");
                doc.Save(writer);
            }
            return !Log.HasLoggedErrors;
        }
    }
}
