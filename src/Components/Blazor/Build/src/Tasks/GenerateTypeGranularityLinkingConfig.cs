// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;
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
            using (var fileStream = File.Open(OutputPath, FileMode.Create))
            using (var xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true }))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteComment(" THIS IS A GENERATED FILE - DO NOT EDIT MANUALLY ");

                xmlWriter.WriteStartElement("linker");

                foreach (var assembly in Assemblies)
                {
                    if (assembly.GetMetadata("TypeGranularity").Equals("true", StringComparison.Ordinal))
                    {
                        AddTypeGranularityConfig(xmlWriter, assembly);
                    }
                }

                xmlWriter.WriteEndElement(); // linker

                xmlWriter.WriteEndDocument();
                xmlWriter.Flush();
            }

            return true;
        }

        private void AddTypeGranularityConfig(XmlWriter xmlWriter, ITaskItem assembly)
        {
            xmlWriter.WriteStartElement("assembly");
            xmlWriter.WriteAttributeString("fullname", Path.GetFileNameWithoutExtension(assembly.ItemSpec));

            xmlWriter.WriteStartElement("type");
            xmlWriter.WriteAttributeString("fullname", "*");
            xmlWriter.WriteAttributeString("preserve", "all");
            xmlWriter.WriteAttributeString("required", "false");
            xmlWriter.WriteEndElement(); // type

            xmlWriter.WriteEndElement(); // assembly
        }
    }
}
