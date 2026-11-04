// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

// Internal for testing
[DebuggerDisplay("{Path}")]
internal class FileNode
{
    public FileNode(string path, RazorProjectItem projectItem)
    {
        Path = path;
        ProjectItem = projectItem;
    }

    public DirectoryNode Directory { get; set; }

    public string Path { get; }

    public RazorProjectItem ProjectItem { get; set; }
}
