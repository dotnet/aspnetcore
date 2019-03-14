// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders
{
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
}