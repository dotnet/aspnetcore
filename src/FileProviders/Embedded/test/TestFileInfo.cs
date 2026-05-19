// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders;

internal class TestFileInfo : IFileInfo
{
    private readonly string _name;
    private readonly bool _isDirectory;

    public TestFileInfo(string name, bool isDirectory)
    {
        _name = name;
        _isDirectory = isDirectory;
    }

    public bool Exists => true;

    public long Length => _isDirectory ? -1 : 0;

    public string PhysicalPath => null;

    public string Name => _name;

    public DateTimeOffset LastModified => throw new NotImplementedException();

    public bool IsDirectory => _isDirectory;

    public Stream CreateReadStream() => Stream.Null;
}
