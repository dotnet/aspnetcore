// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class ManifestDirectoryInfo : IFileInfo
{
    public ManifestDirectoryInfo(ManifestDirectory directory, DateTimeOffset lastModified)
    {
        ArgumentNullThrowHelper.ThrowIfNull(directory);

        Directory = directory;
        LastModified = lastModified;
    }

    public bool Exists => true;

    public long Length => -1;

    public string? PhysicalPath => null;

    public string Name => Directory.Name;

    public DateTimeOffset LastModified { get; }

    public bool IsDirectory => true;

    public ManifestDirectory Directory { get; }

    public Stream CreateReadStream() =>
        throw new InvalidOperationException("Cannot create a stream for a directory.");
}
