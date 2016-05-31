// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Watcher.Core.Internal
{
    public class ProjectWatcher : IDisposable
    {
        private readonly IProjectProvider _projectProvider;
        private readonly IFileWatcher _fileWatcher;

        private readonly string _rootProject;
        private readonly bool _watchProjectJsonOnly;

        private ISet<string> _watchedFiles;

        public ProjectWatcher(
            string projectToWatch,
            bool watchProjectJsonOnly,
            Func<IFileWatcher> fileWatcherFactory,
            IProjectProvider projectProvider)
        {
            _projectProvider = projectProvider;
            _fileWatcher = fileWatcherFactory();

            _rootProject = projectToWatch;
            _watchProjectJsonOnly = watchProjectJsonOnly;
        }

        public async Task<string> WaitForChangeAsync(CancellationToken cancellationToken)
        {
            _watchedFiles = GetProjectFilesClosure(_rootProject);

            foreach (var file in _watchedFiles)
            {
                _fileWatcher.WatchDirectory(Path.GetDirectoryName(file));
            }

            var tcs = new TaskCompletionSource<string>();
            cancellationToken.Register(() => tcs.TrySetResult(null));

            Action<string> callback = path =>
            {
                // If perf becomes a problem, this could be a good starting point
                // because it reparses the project on every change
                // Maybe it could time-buffer the changes in case there are a lot
                // of files changed at the same time
                if (IsFileInTheWatchedSet(path))
                {
                    tcs.TrySetResult(path);
                }
            };

            _fileWatcher.OnFileChange += callback;
            var changedFile = await tcs.Task;
            _fileWatcher.OnFileChange -= callback;

            return changedFile;
        }

        public void Dispose()
        {
            _fileWatcher?.Dispose();
        }

        private bool IsFileInTheWatchedSet(string file)
        {
            // If the file was already watched
            // or if the new project file closure determined
            // by file globbing patterns contains the new file
            // Note, we cannot simply rebuild the closure every time because it wouldn't
            // detect renamed files that have the new name outside of the closure
            return
                _watchedFiles.Contains(file) ||
                GetProjectFilesClosure(_rootProject).Contains(file);
        }

        private ISet<string> GetProjectFilesClosure(string projectFile)
        {
            var closure = new HashSet<string>();

            if (_watchProjectJsonOnly)
            {
                closure.Add(projectFile);
            }
            else
            {
                GetProjectFilesClosure(projectFile, closure);
            }
            return closure;
        }

        private void GetProjectFilesClosure(string projectFile, ISet<string> closure)
        {
            closure.Add(projectFile);

            IProject project;
            string errors;

            if (_projectProvider.TryReadProject(projectFile, out project, out errors))
            {
                foreach (var file in project.Files)
                {
                    closure.Add(file);
                }

                foreach (var dependency in project.ProjectDependencies)
                {
                    GetProjectFilesClosure(dependency, closure);
                }
            }
        }
    }
}
