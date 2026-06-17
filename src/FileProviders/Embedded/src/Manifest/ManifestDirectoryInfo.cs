// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class ManifestDirectoryInfo : IFileInfo, IDirectoryContents
{
    private IFileInfo[]? _entries;

    public ManifestDirectoryInfo(Assembly assembly, ManifestDirectory directory, DateTimeOffset lastModified)
    {
        ArgumentNullThrowHelper.ThrowIfNull(assembly);
        ArgumentNullThrowHelper.ThrowIfNull(directory);

        Assembly = assembly;
        Directory = directory;
        LastModified = lastModified;
    }

    public Assembly Assembly { get; }

    public bool Exists => true;

    public long Length => -1;

    public string? PhysicalPath => null;

    public string Name => Directory.Name;

    public DateTimeOffset LastModified { get; }

    public bool IsDirectory => true;

    public ManifestDirectory Directory { get; }

    public Stream CreateReadStream() =>
        throw new InvalidOperationException("Cannot create a stream for a directory.");

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        return EnsureEntries().GetEnumerator();

        IEnumerable<IFileInfo> EnsureEntries() => _entries ??= ResolveEntries().ToArray();

        IEnumerable<IFileInfo> ResolveEntries()
        {
            if (Directory == ManifestEntry.UnknownPath)
            {
                return Array.Empty<IFileInfo>();
            }

            var entries = new List<IFileInfo>();

            foreach (var entry in Directory.Children)
            {
                IFileInfo fileInfo = entry switch
                {
                    ManifestFile file => new ManifestFileInfo(Assembly, file, LastModified),
                    ManifestDirectory directory => new ManifestDirectoryInfo(Assembly, directory, LastModified),
                    _ => throw new InvalidOperationException("Unknown entry type")
                };

                entries.Add(fileInfo);
            }

            return entries;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
