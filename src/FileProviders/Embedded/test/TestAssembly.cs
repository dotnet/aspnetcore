// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.FileProviders.Embedded.Manifest;

namespace Microsoft.Extensions.FileProviders
{
    internal class TestAssembly : Assembly
    {
        public TestAssembly(string manifest, string manifestName = "Microsoft.Extensions.FileProviders.Embedded.Manifest.xml")
        {
            ManifestStream = new MemoryStream();
            using (var writer = new StreamWriter(ManifestStream, Encoding.UTF8, 1024, leaveOpen: true))
            {
                writer.Write(manifest);
            }

            ManifestStream.Seek(0, SeekOrigin.Begin);
            ManifestName = manifestName;
        }

        public TestAssembly(TestEntry entry, string manifestName = "Microsoft.Extensions.FileProviders.Embedded.Manifest.xml")
        {
            ManifestName = manifestName;

            var manifest = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Manifest",
                    new XElement("ManifestVersion", "1.0"),
                    new XElement("FileSystem", entry.Children.Select(c => c.ToXElement()))));

            ManifestStream = new MemoryStream();
            using (var writer = XmlWriter.Create(ManifestStream, new XmlWriterSettings { CloseOutput = false }))
            {
                manifest.WriteTo(writer);
            }

            ManifestStream.Seek(0, SeekOrigin.Begin);
            Files = entry.GetFiles().Select(f => f.ResourcePath).ToArray();
        }

        public string ManifestName { get; }
        public MemoryStream ManifestStream { get; private set; }
        public string[] Files { get; private set; }

        public override Stream GetManifestResourceStream(string name)
        {
            if (string.Equals(ManifestName, name))
            {
                return ManifestStream;
            }

            return Files.Contains(name) ? Stream.Null : null;
        }

        public override string Location => null;

        public override AssemblyName GetName()
        {
            return new AssemblyName("TestAssembly");
        }
    }
}
