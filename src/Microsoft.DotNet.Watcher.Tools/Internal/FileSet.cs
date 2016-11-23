// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class FileSet : IFileSet
    {
        private readonly HashSet<string> _files;

        public FileSet(IEnumerable<string> files)
        {
            _files = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        }

        public bool Contains(string filePath) => _files.Contains(filePath);

        public int Count => _files.Count;

        public IEnumerator<string> GetEnumerator() => _files.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _files.GetEnumerator();
    }
}
