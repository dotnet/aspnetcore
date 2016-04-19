// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.DotNet.Watcher.Core.Internal;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class WaitForFileToChange : IDisposable
    {
        private readonly IFileSystemWatcher _watcher;
        private readonly string _expectedFile;

        private ManualResetEvent _changed = new ManualResetEvent(false);

        public WaitForFileToChange(string file)
        {
            _watcher = FileWatcherFactory.CreateWatcher(Path.GetDirectoryName(file), usePollingWatcher: true);
            _expectedFile = file;

            _watcher.OnFileChange += WatcherEvent;
            
            _watcher.EnableRaisingEvents = true;
        }

        private void WatcherEvent(object sender, string file)
        {
            if (file.Equals(_expectedFile, StringComparison.Ordinal))
            {
                Waiters.WaitForFileToBeReadable(_expectedFile, TimeSpan.FromSeconds(10));
                _changed?.Set();
            }
        }

        public void Wait(TimeSpan timeout, bool expectedToChange, string errorMessage)
        {
            if (_changed != null)
            {
                var changed = _changed.WaitOne(timeout);
                if (changed != expectedToChange)
                {
                    throw new Exception(errorMessage);
                }
            }
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;

            _watcher.OnFileChange -= WatcherEvent;
            
            _watcher.Dispose();
            _changed.Dispose();

            _changed = null;
        }
    }
}
