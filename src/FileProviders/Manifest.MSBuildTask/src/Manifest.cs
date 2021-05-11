// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest.Task
{
    public class Manifest
    {
        public Entry Root { get; set; } = Entry.Directory("");

        public void AddElement(string originalPath, string assemblyResourceName)
        {
            if (originalPath == null)
            {
                throw new System.ArgumentNullException(nameof(originalPath));
            }

            if (assemblyResourceName == null)
            {
                throw new System.ArgumentNullException(nameof(assemblyResourceName));
            }

            var paths = originalPath.Split(Path.DirectorySeparatorChar);
            var current = Root;
            for (int i = 0; i < paths.Length - 1; i++)
            {
                var currentSegment = paths[i];
                var next = current.GetDirectory(currentSegment);
                if (next == null)
                {
                    next = Entry.Directory(currentSegment);
                    current.AddChild(next);
                }
                current = next;
            }

            current.AddChild(Entry.File(paths[paths.Length - 1], assemblyResourceName));
        }

        public XDocument ToXmlDocument()
        {
            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            var root = new XElement(ElementNames.Root,
                new XElement(ElementNames.ManifestVersion, "1.0"),
                new XElement(ElementNames.FileSystem,
                Root.Children.Select(e => BuildNode(e))));

            document.Add(root);

            return document;
        }

        private XElement BuildNode(Entry entry)
        {
            if (entry.IsFile)
            {
                return new XElement(ElementNames.File,
                    new XAttribute(ElementNames.Name, entry.Name),
                    new XElement(ElementNames.ResourcePath, entry.AssemblyResourceName));
            }
            else
            {
                var directory = new XElement(ElementNames.Directory, new XAttribute(ElementNames.Name, entry.Name));
                directory.Add(entry.Children.Select(c => BuildNode(c)));
                return directory;
            }
        }

        private class ElementNames
        {
            public static readonly string Directory = "Directory";
            public static readonly string Name = "Name";
            public static readonly string FileSystem = "FileSystem";
            public static readonly string Root = "Manifest";
            public static readonly string File = "File";
            public static readonly string ResourcePath = "ResourcePath";
            public static readonly string ManifestVersion = "ManifestVersion";
        }
    }
}
