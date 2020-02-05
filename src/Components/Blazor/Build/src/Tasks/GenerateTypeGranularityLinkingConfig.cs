// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Blazor.Build.Tasks
{
    public class GenerateTypeGranularityLinkingConfig : Task
    {
        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            var linkerElement = new XElement("linker",
                new XComment(" THIS IS A GENERATED FILE - DO NOT EDIT MANUALLY "));

            foreach (var assembly in Assemblies)
            {
                var assemblyElement = CreateTypeGranularityConfig(assembly);
                linkerElement.Add(assemblyElement);
            }

            var contentsToWrite = new XDocument(linkerElement).ToString();
            if (File.Exists(OutputPath))
            {
                var existingContent = File.ReadAllText(OutputPath);
                if (string.Equals(contentsToWrite, existingContent, StringComparison.Ordinal))
                {
                    Log.LogMessage(MessageImportance.Low, "Skipping unchanged file {0}", OutputPath);
                    return true;
                }
            }

            File.WriteAllText(OutputPath, contentsToWrite);
            return true;
        }

        private XElement CreateTypeGranularityConfig(ITaskItem assembly)
        {
            // We match all types in the assembly, and for each one, tell the linker to preserve all
            // its members (preserve=all) but only if there's some reference to the type (required=false)
            return new XElement("assembly",
                new XAttribute("fullname", Path.GetFileNameWithoutExtension(assembly.ItemSpec)),
                new XElement("type",
                    new XAttribute("fullname", "*"),
                    new XAttribute("preserve", "all"),
                    new XAttribute("required", "false")));
        }
    }
}
