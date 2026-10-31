// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest;

internal sealed class ManifestFileInfo : IFileInfo
{
    private long? _length;

    public ManifestFileInfo(Assembly assembly, ManifestFile file, DateTimeOffset lastModified)
    {
        ArgumentNullThrowHelper.ThrowIfNull(assembly);
        ArgumentNullThrowHelper.ThrowIfNull(file);

        Assembly = assembly;
        ManifestFile = file;
        LastModified = lastModified;
    }

    public Assembly Assembly { get; }

    public ManifestFile ManifestFile { get; }

    public bool Exists => true;

    public long Length => EnsureLength();

    public string? PhysicalPath => null;

    public string Name => ManifestFile.Name;

    public DateTimeOffset LastModified { get; }

    public bool IsDirectory => false;

    private long EnsureLength()
    {
        if (_length == null)
        {
            using var stream = GetManifestResourceStream();
            _length = stream.Length;
        }

        return _length.Value;
    }

    public Stream CreateReadStream()
    {
        var stream = GetManifestResourceStream();
        if (!_length.HasValue)
        {
            _length = stream.Length;
        }

        return stream;
    }

    private Stream GetManifestResourceStream()
    {
        var stream = Assembly.GetManifestResourceStream(ManifestFile.ResourcePath);
        if (stream == null)
        {
            throw new InvalidOperationException($"Couldn't get resource at '{ManifestFile.ResourcePath}'.");
        }

        return stream;
    }
}
