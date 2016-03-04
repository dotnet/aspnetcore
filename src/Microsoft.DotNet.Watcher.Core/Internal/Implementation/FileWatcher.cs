// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Watcher.Core.Internal
{
    public class FileWatcher : IFileWatcher
    {
        private bool _disposed;

        private readonly IDictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>();

        public event Action<string> OnFileChange;

        public void WatchDirectory(string directory)
        {
            EnsureNotDisposed();
            AddDirectoryWatcher(directory);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var watcher in _watchers)
            {
                watcher.Value.Dispose();
            }
            _watchers.Clear();
        }

        private void AddDirectoryWatcher(string directory)
        {
            var alreadyWatched = _watchers
                .Where(d => directory.StartsWith(d.Key))
                .Any();

            if (alreadyWatched)
            {
                return;
            }

            var redundantWatchers = _watchers
                .Where(d => d.Key.StartsWith(directory))
                .Select(d => d.Key)
                .ToList();

            if (redundantWatchers.Any())
            {
                foreach (var watcher in redundantWatchers)
                {
                    DisposeWatcher(watcher);
                }
            }

            var newWatcher = new FileSystemWatcher(directory);
            newWatcher.IncludeSubdirectories = true;

            newWatcher.Changed += WatcherChangedHandler;
            newWatcher.Created += WatcherChangedHandler;
            newWatcher.Deleted += WatcherChangedHandler;
            newWatcher.Renamed += WatcherRenamedHandler;

            newWatcher.EnableRaisingEvents = true;

            _watchers.Add(directory, newWatcher);
        }

        private void WatcherRenamedHandler(object sender, RenamedEventArgs e)
        {
            NotifyChange(e.OldFullPath);
            NotifyChange(e.FullPath);
        }

        private void WatcherChangedHandler(object sender, FileSystemEventArgs e)
        {
            NotifyChange(e.FullPath);
        }

        private void NotifyChange(string path)
        {
            if (OnFileChange != null)
            {
                OnFileChange(path);
            }
        }

        private void DisposeWatcher(string directory)
        {
            var watcher = _watchers[directory];
            _watchers.Remove(directory);

            watcher.EnableRaisingEvents = false;

            watcher.Changed -= WatcherChangedHandler;
            watcher.Created -= WatcherChangedHandler;
            watcher.Deleted -= WatcherChangedHandler;
            watcher.Renamed -= WatcherRenamedHandler;

            watcher.Dispose();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileWatcher));
            }
        }
    }
}