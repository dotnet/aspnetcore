// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.FileProviders;

public class TestDirectoryFileInfo : IFileInfo
{
    public bool IsDirectory => true;

    public long Length { get; set; }

    public string Name { get; set; }

    public string PhysicalPath { get; set; }

    public bool Exists => true;

    public DateTimeOffset LastModified => throw new NotImplementedException();

    public Stream CreateReadStream()
    {
        throw new NotSupportedException();
    }
}
