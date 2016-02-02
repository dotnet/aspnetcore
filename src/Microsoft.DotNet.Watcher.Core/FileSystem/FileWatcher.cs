// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Watcher.Core
{ 
    public class FileWatcher : IFileWatcher
    {
        private readonly HashSet<string> _files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _directories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly List<IWatcherRoot> _watchers = new List<IWatcherRoot>();

        internal FileWatcher()
        {
        }

        public FileWatcher(string path)
        {
            AddWatcher(path);
        }

        public event Action<string> OnChanged;

        public void WatchDirectory(string path, string extension)
        {
            var extensions = _directories.GetOrAdd(path, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            extensions.Add(extension);
        }

        public bool WatchFile(string path)
        {
            return _files.Add(path);
        }

        public void WatchProject(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return;
            }

            // If any watchers already handle this path then noop
            if (!IsAlreadyWatched(projectPath))
            {
                // To reduce the number of watchers we have we add a watcher to the root
                // of this project so that we'll be notified if anything we care
                // about changes
                var rootPath = ResolveRootDirectory(projectPath);
                AddWatcher(rootPath);
            }
        }

        // For testing
        internal bool IsAlreadyWatched(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return false;
            }

            bool anyWatchers = false;

            foreach (var watcher in _watchers)
            {
                // REVIEW: This needs to work x-platform, should this be case
                // sensitive?
                if (EnsureTrailingSlash(projectPath).StartsWith(EnsureTrailingSlash(watcher.Path), StringComparison.OrdinalIgnoreCase))
                {
                    anyWatchers = true;
                }
            }

            return anyWatchers;
        }

        public void Dispose()
        {
            foreach (var w in _watchers)
            {
                w.Dispose();
            }

            _watchers.Clear();
        }

        public bool ReportChange(string newPath, WatcherChangeTypes changeType)
        {
            return ReportChange(oldPath: null, newPath: newPath, changeType: changeType);
        }

        public bool ReportChange(string oldPath, string newPath, WatcherChangeTypes changeType)
        {
            if (HasChanged(oldPath, newPath, changeType))
            {
                if (OnChanged != null)
                {
                    OnChanged(oldPath ?? newPath);
                }

                return true;
            }

            return false;
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        // For testing only
        internal void AddWatcher(IWatcherRoot watcherRoot)
        {
            _watchers.Add(watcherRoot);
        }

        private void AddWatcher(string path)
        {
            var watcher = new FileSystemWatcher(path);
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            watcher.Changed += OnWatcherChanged;
            watcher.Renamed += OnRenamed;
            watcher.Deleted += OnWatcherChanged;
            watcher.Created += OnWatcherChanged;

            _watchers.Add(new FileSystemWatcherRoot(watcher));
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            ReportChange(e.OldFullPath, e.FullPath, e.ChangeType);
        }

        private void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {
            ReportChange(e.FullPath, e.ChangeType);
        }

        private bool HasChanged(string oldPath, string newPath, WatcherChangeTypes changeType)
        {
            // File changes
            if (_files.Contains(newPath) ||
                (oldPath != null && _files.Contains(oldPath)))
            {
                return true;
            }

            HashSet<string> extensions;
            if (_directories.TryGetValue(newPath, out extensions) ||
                _directories.TryGetValue(Path.GetDirectoryName(newPath), out extensions))
            {
                string extension = Path.GetExtension(newPath);

                if (String.IsNullOrEmpty(extension))
                {
                    // Assume it's a directory
                    if (changeType == WatcherChangeTypes.Created ||
                        changeType == WatcherChangeTypes.Renamed)
                    {
                        foreach (var e in extensions)
                        {
                            WatchDirectory(newPath, e);
                        }
                    }
                    else if (changeType == WatcherChangeTypes.Deleted)
                    {
                        return true;
                    }

                    // Ignore anything else
                    return false;
                }

                return extensions.Contains(extension);
            }

            return false;
        }

        private static string ResolveRootDirectory(string projectPath)
        {
            var di = new DirectoryInfo(projectPath);

            while (di.Parent != null)
            {
                var globalJsonPath = Path.Combine(di.FullName, "global.json");

                if (File.Exists(globalJsonPath))
                {
                    return di.FullName;
                }

                di = di.Parent;
            }

            // If we don't find any files then make the project folder the root
            return projectPath;
        }
    }
}