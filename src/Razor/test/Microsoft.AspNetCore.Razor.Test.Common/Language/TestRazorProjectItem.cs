// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language;

public class TestRazorProjectItem : RazorProjectItem
{
    private readonly string _fileKind;

    public TestRazorProjectItem(
        string filePath,
        string physicalPath = null,
        string relativePhysicalPath = null,
        string basePath = "/",
        string fileKind = null,
        string cssScope = null)
    {
        FilePath = filePath;
        PhysicalPath = physicalPath;
        RelativePhysicalPath = relativePhysicalPath;
        BasePath = basePath;
        CssScope = cssScope;
        _fileKind = fileKind;
    }

    public override string BasePath { get; }

    public override string FileKind => _fileKind ?? base.FileKind;

    public override string FilePath { get; }

    public override string PhysicalPath { get; }

    public override string RelativePhysicalPath { get; }

    public override string CssScope { get; }

    public override bool Exists { get; } = true;

    public string Content { get; set; } = "Default content";

    public override Stream Read()
    {
        // Act like a file and have a UTF8 BOM.
        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(Content);
        var buffer = new byte[preamble.Length + contentBytes.Length];
        preamble.CopyTo(buffer, 0);
        contentBytes.CopyTo(buffer, preamble.Length);

        var stream = new MemoryStream(buffer);

        return stream;
    }
}
