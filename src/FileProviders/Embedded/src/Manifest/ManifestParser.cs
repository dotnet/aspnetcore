// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal static class ManifestParser
{
    private const string DefaultManifestName = "Microsoft.Extensions.FileProviders.Embedded.Manifest.xml";

    public static EmbeddedFilesManifest Parse(Assembly assembly)
    {
        return Parse(assembly, DefaultManifestName);
    }

    public static EmbeddedFilesManifest Parse(Assembly assembly, string name)
    {
        ArgumentNullThrowHelper.ThrowIfNull(assembly);
        ArgumentNullThrowHelper.ThrowIfNull(name);

        var stream = assembly.GetManifestResourceStream(name);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not load the embedded file manifest " +
                $"'{name}' for assembly '{assembly.GetName().Name}'.");
        }

        var document = XDocument.Load(stream);

        var manifest = EnsureElement(document, "Manifest");
        var manifestVersion = EnsureElement(manifest, "ManifestVersion");
        var version = EnsureText(manifestVersion);
        if (!string.Equals("1.0", version, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The embedded file manifest '{name}' for " +
                $"assembly '{assembly.GetName().Name}' specifies an unsupported file format" +
                $" version: '{version}'.");
        }
        var fileSystem = EnsureElement(manifest, "FileSystem");

        var entries = fileSystem.Elements();
        var entriesList = new List<ManifestEntry>();
        foreach (var element in entries)
        {
            var entry = BuildEntry(element);
            entriesList.Add(entry);
        }

        ValidateEntries(entriesList);

        var rootDirectory = ManifestDirectory.CreateRootDirectory(entriesList.ToArray());

        return new EmbeddedFilesManifest(rootDirectory);
    }

    private static void ValidateEntries(List<ManifestEntry> entriesList)
    {
        for (int i = 0; i < entriesList.Count - 1; i++)
        {
            for (int j = i + 1; j < entriesList.Count; j++)
            {
                if (string.Equals(entriesList[i].Name, entriesList[j].Name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Found two entries with the same name but different casing:" +
                        $" '{entriesList[i].Name}' and '{entriesList[j]}'");
                }
            }
        }
    }

    private static ManifestEntry BuildEntry(XElement element)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        if (element.NodeType != XmlNodeType.Element)
        {
            throw new InvalidOperationException($"Invalid manifest format. Expected a 'File' or a 'Directory' node:" +
                $" '{element}'");
        }

        if (string.Equals(element.Name.LocalName, "File", StringComparison.Ordinal))
        {
            var entryName = EnsureName(element);
            var path = EnsureElement(element, "ResourcePath");
            var pathValue = EnsureText(path);
            return new ManifestFile(entryName, pathValue);
        }

        if (string.Equals(element.Name.LocalName, "Directory", StringComparison.Ordinal))
        {
            var directoryName = EnsureName(element);
            var children = new List<ManifestEntry>();
            foreach (var child in element.Elements())
            {
                children.Add(BuildEntry(child));
            }

            ValidateEntries(children);

            return ManifestDirectory.CreateDirectory(directoryName, children.ToArray());
        }

        throw new InvalidOperationException($"Invalid manifest format.Expected a 'File' or a 'Directory' node. " +
            $"Got '{element.Name.LocalName}' instead.");
    }

    private static XElement EnsureElement(XContainer container, string elementName)
    {
        var element = container.Element(elementName);
        if (element == null)
        {
            throw new InvalidOperationException($"Invalid manifest format. Missing '{elementName}' element name");
        }

        return element;
    }

    private static string EnsureName(XElement element)
    {
        var value = element.Attribute("Name")?.Value;
        if (value == null)
        {
            throw new InvalidOperationException($"Invalid manifest format. '{element.Name}' must contain a 'Name' attribute.");
        }

        return value;
    }

    private static string EnsureText(XElement element)
    {
        if (!element.Elements().Any() &&
            !element.IsEmpty &&
            element.Nodes().Count() == 1 &&
            element.FirstNode?.NodeType == XmlNodeType.Text)
        {
            return element.Value;
        }

        throw new InvalidOperationException(
            $"Invalid manifest format. '{element.Name.LocalName}' must contain " +
            $"a text value. '{element.Value}'");
    }
}
