// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class VirtualProjectItem : RazorProjectItem
    {
        private readonly byte[] _content;

        public VirtualProjectItem(string basePath, string filePath, string physicalPath, string relativePhysicalPath, byte[] content)
        {
            BasePath = basePath;
            FilePath = filePath;
            PhysicalPath = physicalPath;
            RelativePhysicalPath = relativePhysicalPath;
            _content = content;
        }

        public override string BasePath { get; }

        public override string RelativePhysicalPath { get; }

        public override string FilePath { get; }

        public override string PhysicalPath { get; }

        public override bool Exists => true;

        public override Stream Read()
        {
            return new MemoryStream(_content);
        }
    }
}
