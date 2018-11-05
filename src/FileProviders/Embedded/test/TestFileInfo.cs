// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    internal class TestFileInfo : IFileInfo
        {
            private readonly string _name;
            private readonly bool _isDirectory;

            public TestFileInfo(string name, bool isDirectory)
            {
                _name = name;
                _isDirectory = isDirectory;
            }

            public bool Exists => true;

            public long Length => _isDirectory ? -1 : 0;

            public string PhysicalPath => null;

            public string Name => _name;

            public DateTimeOffset LastModified => throw new NotImplementedException();

            public bool IsDirectory => _isDirectory;

            public Stream CreateReadStream() => Stream.Null;
        }
}
