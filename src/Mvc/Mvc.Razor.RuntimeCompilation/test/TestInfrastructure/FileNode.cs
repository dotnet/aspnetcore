// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language
{
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
}