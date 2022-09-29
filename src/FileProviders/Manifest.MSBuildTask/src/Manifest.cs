// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest.Task;

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
            Root.Children.Select(BuildNode)));

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
            directory.Add(entry.Children.Select(BuildNode));
            return directory;
        }
    }

    private sealed class ElementNames
    {
        public const string Directory = "Directory";
        public const string Name = "Name";
        public const string FileSystem = "FileSystem";
        public const string Root = "Manifest";
        public const string File = "File";
        public const string ResourcePath = "ResourcePath";
        public const string ManifestVersion = "ManifestVersion";
    }
}
