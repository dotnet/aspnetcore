// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    // Based on https://github.com/mono/linker/blob/3b329b9481e300bcf4fb88a2eebf8cb5ef8b323b/src/ILLink.Tasks/CreateRootDescriptorFile.cs
    public class CreateBlazorTrimmerRootDescriptorFile : Task
    {
        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Required]
        public ITaskItem TrimmerFile { get; set; }

        public override bool Execute()
        {
            using var fileStream = File.Create(TrimmerFile.ItemSpec);

            WriteRootDescriptor(fileStream);
            return true;
        }

        internal void WriteRootDescriptor(Stream stream)
        {
            var roots = new XElement("linker");
            foreach (var assembly in Assemblies)
            {
                var assemblyName = assembly.GetMetadata("FileName") + assembly.GetMetadata("Extension");
                var typePreserved = assembly.GetMetadata("Preserve");
                var typeRequired = assembly.GetMetadata("Required");

                var attributes = new List<XAttribute>
                {
                    new XAttribute("fullname", "*"),
                    new XAttribute("required", typeRequired),
                };

                if (!string.IsNullOrEmpty(typePreserved))
                {
                    attributes.Add(new XAttribute("preserve", typePreserved));
                }

                roots.Add(new XElement("assembly",
                    new XAttribute("fullname", assemblyName),
                    new XElement("type", attributes)));
            }

            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            };

            using var writer = XmlWriter.Create(stream, xmlWriterSettings);
            var xDocument = new XDocument(roots);

            xDocument.Save(writer);
        }
    }
}
