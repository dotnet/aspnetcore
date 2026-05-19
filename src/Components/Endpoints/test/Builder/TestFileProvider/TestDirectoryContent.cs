// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.Extensions.FileProviders;

public class TestDirectoryContent : IDirectoryContents, IFileInfo
{
    private readonly IEnumerable<IFileInfo> _files;

    public TestDirectoryContent(string name, IEnumerable<IFileInfo> files)
    {
        Name = name;
        _files = files;
    }

    public bool Exists => true;

    public long Length => throw new NotSupportedException();

    public string PhysicalPath => throw new NotSupportedException();

    public string Name { get; }

    public DateTimeOffset LastModified => throw new NotSupportedException();

    public bool IsDirectory => true;

    public Stream CreateReadStream()
    {
        throw new NotSupportedException();
    }

    public IEnumerator<IFileInfo> GetEnumerator() => _files.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
