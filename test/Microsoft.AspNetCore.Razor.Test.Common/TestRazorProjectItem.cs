// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TestRazorProjectItem : RazorProjectItem
    {
        public TestRazorProjectItem(
            string path, 
            string physicalPath = null,
            string basePath = "/")
        {
            Path = path;
            PhysicalPath = physicalPath;
            BasePath = basePath;
        }

        public override string BasePath { get; }

        public override string Path { get; }

        public override string PhysicalPath { get; }

        public override bool Exists => true;

        public string Content { get; set; } = "Default content";

        public override Stream Read() => new MemoryStream(Encoding.UTF8.GetBytes(Content));
    }
}
