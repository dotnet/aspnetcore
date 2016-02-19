// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.DotNet.Watcher.FunctionalTests
{
    public class WaitForFileToChange : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly string _expectedFile;

        private readonly ManualResetEvent _changed = new ManualResetEvent(false);

        public WaitForFileToChange(string file)
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(file), "*" + Path.GetExtension(file));
            _expectedFile = file;

            _watcher.Changed += WatcherEvent;
            _watcher.Created += WatcherEvent;

            _watcher.EnableRaisingEvents = true;
        }

        private void WatcherEvent(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Equals(_expectedFile, StringComparison.Ordinal))
            {
                Waiters.WaitForFileToBeReadable(_expectedFile, TimeSpan.FromSeconds(10));
                _changed.Set();
            }
        }

        public void Wait(TimeSpan timeout, bool expectedToChange, string errorMessage)
        {
            var changed = _changed.WaitOne(timeout);
            if (changed != expectedToChange)
            {
                throw new Exception(errorMessage);
            }
        }

        public void Dispose()
        {
            _watcher.Dispose();
            _changed.Dispose();
        }
    }
}
