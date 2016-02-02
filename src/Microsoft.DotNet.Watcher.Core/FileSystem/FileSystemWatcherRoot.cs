// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.DotNet.Watcher.Core
{
    internal class FileSystemWatcherRoot : IWatcherRoot
    {
        private readonly FileSystemWatcher _watcher;

        public FileSystemWatcherRoot(FileSystemWatcher watcher)
        {
            _watcher = watcher;
        }

        public string Path
        {
            get
            {
                return _watcher.Path;
            }
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}