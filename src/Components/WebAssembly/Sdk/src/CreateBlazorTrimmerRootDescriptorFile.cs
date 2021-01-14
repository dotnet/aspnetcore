// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.BlazorWebAssembly
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
            var rootDescriptor = CreateRootDescriptorContents();
            if (File.Exists(TrimmerFile.ItemSpec))
            {
                var existing = File.ReadAllText(TrimmerFile.ItemSpec);

                if (string.Equals(rootDescriptor, existing, StringComparison.Ordinal))
                {
                    Log.LogMessage(MessageImportance.Low, "Skipping write to file {0} because contents would not change.", TrimmerFile.ItemSpec);
                    // Avoid writing if the file contents are identical. This is required for build incrementalism.
                    return !Log.HasLoggedErrors;
                }
            }

            File.WriteAllText(TrimmerFile.ItemSpec, rootDescriptor);
            return !Log.HasLoggedErrors;
        }

        internal string CreateRootDescriptorContents()
        {
            var roots = new XElement("linker");
            foreach (var assembly in Assemblies.OrderBy(a => a.ItemSpec))
            {
                // NOTE: Descriptor files don't include the file extension
                // in the assemblyName.
                var assemblyName = assembly.GetMetadata("FileName");
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

            return new XDocument(roots).Root.ToString();
        }
    }
}
