// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Internal
{
    internal class DotnetFileWatcher : IFileSystemWatcher
    {
        private volatile bool _disposed;

        private readonly Func<string, FileSystemWatcher> _watcherFactory;

        private FileSystemWatcher _fileSystemWatcher;

        private readonly object _createLock = new object();

        public DotnetFileWatcher(string watchedDirectory)
            : this(watchedDirectory, DefaultWatcherFactory)
        {
        }

        internal DotnetFileWatcher(string watchedDirectory, Func<string, FileSystemWatcher> fileSystemWatcherFactory)
        {
            Ensure.NotNull(fileSystemWatcherFactory, nameof(fileSystemWatcherFactory));
            Ensure.NotNullOrEmpty(watchedDirectory, nameof(watchedDirectory));

            BasePath = watchedDirectory;
            _watcherFactory = fileSystemWatcherFactory;
            CreateFileSystemWatcher();
        }

        public event EventHandler<string> OnFileChange;

        public event EventHandler<Exception> OnError;

        public string BasePath { get; }

        private static FileSystemWatcher DefaultWatcherFactory(string watchedDirectory)
        {
            Ensure.NotNullOrEmpty(watchedDirectory, nameof(watchedDirectory));

            return new FileSystemWatcher(watchedDirectory);
        }

        private void WatcherErrorHandler(object sender, ErrorEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            var exception = e.GetException();

            // Win32Exception may be triggered when setting EnableRaisingEvents on a file system type
            // that is not supported, such as a network share. Don't attempt to recreate the watcher
            // in this case as it will cause a StackOverflowException
            if (!(exception is Win32Exception))
            {
                // Recreate the watcher if it is a recoverable error.
                CreateFileSystemWatcher();
            }

            OnError?.Invoke(this, exception);
        }

        private void WatcherRenameHandler(object sender, RenamedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            NotifyChange(e.OldFullPath);
            NotifyChange(e.FullPath);

            if (Directory.Exists(e.FullPath))
            {
                foreach (var newLocation in Directory.EnumerateFileSystemEntries(e.FullPath, "*", SearchOption.AllDirectories))
                {
                    // Calculated previous path of this moved item.
                    var oldLocation = Path.Combine(e.OldFullPath, newLocation.Substring(e.FullPath.Length + 1));
                    NotifyChange(oldLocation);
                    NotifyChange(newLocation);
                }
            }
        }

        private void WatcherChangeHandler(object sender, FileSystemEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            NotifyChange(e.FullPath);
        }

        private void NotifyChange(string fullPath)
        {
            // Only report file changes
            OnFileChange?.Invoke(this, fullPath);
        }

        private void CreateFileSystemWatcher()
        {
            lock (_createLock)
            {
                bool enableEvents = false;

                if (_fileSystemWatcher != null)
                {
                    enableEvents = _fileSystemWatcher.EnableRaisingEvents;

                    DisposeInnerWatcher();
                }

                _fileSystemWatcher = _watcherFactory(BasePath);
                _fileSystemWatcher.IncludeSubdirectories = true;

                _fileSystemWatcher.Created += WatcherChangeHandler;
                _fileSystemWatcher.Deleted += WatcherChangeHandler;
                _fileSystemWatcher.Changed += WatcherChangeHandler;
                _fileSystemWatcher.Renamed += WatcherRenameHandler;
                _fileSystemWatcher.Error += WatcherErrorHandler;

                _fileSystemWatcher.EnableRaisingEvents = enableEvents;
            }
        }

        private void DisposeInnerWatcher()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;

            _fileSystemWatcher.Created -= WatcherChangeHandler;
            _fileSystemWatcher.Deleted -= WatcherChangeHandler;
            _fileSystemWatcher.Changed -= WatcherChangeHandler;
            _fileSystemWatcher.Renamed -= WatcherRenameHandler;
            _fileSystemWatcher.Error -= WatcherErrorHandler;

            _fileSystemWatcher.Dispose();
        }

        public bool EnableRaisingEvents
        {
            get => _fileSystemWatcher.EnableRaisingEvents;
            set => _fileSystemWatcher.EnableRaisingEvents = value;
        }

        public void Dispose()
        {
            _disposed = true;
            DisposeInnerWatcher();
        }
    }
}
