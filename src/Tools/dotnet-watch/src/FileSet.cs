// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Watcher
{
    public class FileSet : IEnumerable<FileItem>
    {
        private readonly Dictionary<string, FileItem> _files;

        public FileSet(bool isNetCoreApp31OrNewer, IEnumerable<FileItem> files)
        {
            IsNetCoreApp31OrNewer = isNetCoreApp31OrNewer;
            _files = new Dictionary<string, FileItem>(StringComparer.Ordinal);
            foreach (var item in files)
            {
                _files[item.FilePath] = item;
            }
        }

        public bool TryGetValue(string filePath, out FileItem fileItem) => _files.TryGetValue(filePath, out fileItem);

        public int Count => _files.Count;

        public bool IsNetCoreApp31OrNewer { get; }

        public static readonly FileSet Empty = new FileSet(false, Array.Empty<FileItem>());

        public IEnumerator<FileItem> GetEnumerator() => _files.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
