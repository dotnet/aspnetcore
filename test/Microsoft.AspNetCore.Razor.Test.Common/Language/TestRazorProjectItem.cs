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
            FilePath = path;
            PhysicalPath = physicalPath;
            BasePath = basePath;
        }

        public override string BasePath { get; }

        public override string FilePath { get; }

        public override string PhysicalPath { get; }

        public override bool Exists => true;

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
}
