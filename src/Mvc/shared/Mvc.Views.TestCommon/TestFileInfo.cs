// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Extensions.FileProviders;

public class TestFileInfo : IFileInfo
{
    private string _content;

    public bool IsDirectory => false;

    public DateTimeOffset LastModified { get; set; }

    public long Length { get; set; }

    public string Name { get; set; }

    public string PhysicalPath { get; set; }

    public string Content
    {
        get { return _content; }
        set
        {
            _content = value;
            Length = Encoding.UTF8.GetByteCount(Content);
        }
    }

    public bool Exists
    {
        get { return true; }
    }

    public Stream CreateReadStream()
    {
        var bytes = Encoding.UTF8.GetBytes(Content);
        return new MemoryStream(bytes);
    }
}
