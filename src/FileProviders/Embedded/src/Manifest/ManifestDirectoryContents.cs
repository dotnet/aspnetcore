// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class ManifestDirectoryContents : IDirectoryContents
{
    private readonly DateTimeOffset _lastModified;
    private IFileInfo[]? _entries;

    public ManifestDirectoryContents(Assembly assembly, ManifestDirectory directory, DateTimeOffset lastModified)
    {
        ArgumentNullThrowHelper.ThrowIfNull(assembly);
        ArgumentNullThrowHelper.ThrowIfNull(directory);

        Assembly = assembly;
        Directory = directory;
        _lastModified = lastModified;
    }

    public bool Exists => true;

    public Assembly Assembly { get; }

    public ManifestDirectory Directory { get; }

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        return EnsureEntries().GetEnumerator();

        IReadOnlyList<IFileInfo> EnsureEntries() => _entries = _entries ?? ResolveEntries().ToArray();

        IEnumerable<IFileInfo> ResolveEntries()
        {
            if (Directory == ManifestEntry.UnknownPath)
            {
                yield break;
            }

            foreach (var entry in Directory.Children)
            {
                switch (entry)
                {
                    case ManifestFile f:
                        yield return new ManifestFileInfo(Assembly, f, _lastModified);
                        break;
                    case ManifestDirectory d:
                        yield return new ManifestDirectoryInfo(d, _lastModified);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown entry type");
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
