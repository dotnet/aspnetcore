// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Blazor.Build
{
    // Based on https://github.com/mono/linker/blob/3b329b9481e300bcf4fb88a2eebf8cb5ef8b323b/src/ILLink.Tasks/CreateRootDescriptorFile.cs
    public class BlazorCreateRootDescriptorFile : Task
    {
        [Required]
        public ITaskItem[] AssemblyNames { get; set; }

        [Required]
        public ITaskItem RootDescriptorFilePath { get; set; }

        public override bool Execute()
        {
            using var fileStream = File.Create(RootDescriptorFilePath.ItemSpec);
            var assemblyNames = AssemblyNames.Select(a => a.ItemSpec);

            WriteRootDescriptor(fileStream, assemblyNames);
            return true;
        }

        internal static void WriteRootDescriptor(Stream stream, IEnumerable<string> assemblyNames)
        {
            var roots = new XElement("linker");
            foreach (var assemblyName in assemblyNames)
            {
                roots.Add(new XElement("assembly",
                    new XAttribute("fullname", assemblyName),
                    new XElement("type",
                        new XAttribute("fullname", "*"),
                        new XAttribute("required", "true"))));
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
