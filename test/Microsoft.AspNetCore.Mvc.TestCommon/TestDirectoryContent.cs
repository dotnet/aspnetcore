// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.TestCommon
{
    public class TestDirectoryContent : IDirectoryContents
    {
        private readonly IEnumerable<IFileInfo> _files;

        public TestDirectoryContent(IEnumerable<IFileInfo> files)
        {
            _files = files;
        }

        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator() => _files.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
