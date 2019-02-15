// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    public class TestDirectoryContent : IDirectoryContents, IFileInfo
    {
        private readonly IEnumerable<IFileInfo> _files;

        public TestDirectoryContent(string name, IEnumerable<IFileInfo> files)
        {
            Name = name;
            _files = files;
        }

        public bool Exists => true;

        public long Length => throw new NotSupportedException();

        public string PhysicalPath => throw new NotSupportedException();

        public string Name { get; }

        public DateTimeOffset LastModified => throw new NotSupportedException();

        public bool IsDirectory => true;

        public Stream CreateReadStream()
        {
            throw new NotSupportedException();
        }

        public IEnumerator<IFileInfo> GetEnumerator() => _files.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
