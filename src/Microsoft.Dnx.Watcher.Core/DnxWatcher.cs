// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;

namespace Microsoft.Dnx.Watcher.Core
{
    public class DnxWatcher
    {
        private readonly Func<string, IFileWatcher> _fileWatcherFactory;
        private readonly Func<IProcessWatcher> _processWatcherFactory;
        private readonly IProjectProvider _projectProvider;
        private readonly ILoggerFactory _loggerFactory;

        private readonly ILogger _logger;

        public DnxWatcher(
            Func<string, IFileWatcher> fileWatcherFactory,
            Func<IProcessWatcher> processWatcherFactory,
            IProjectProvider projectProvider,
            ILoggerFactory loggerFactory)
        {
            _fileWatcherFactory = fileWatcherFactory;
            _processWatcherFactory = processWatcherFactory;
            _projectProvider = projectProvider;
            _loggerFactory = loggerFactory;

            _logger = _loggerFactory.CreateLogger(nameof(DnxWatcher));
        }
        public async Task WatchAsync(string projectFile, string[] dnxArguments, string workingDir, CancellationToken cancellationToken)
        {
            dnxArguments = new string[] { "--project", projectFile }
                .Concat(dnxArguments)
                .Select(arg =>
                {
                    // If the argument has spaces, make sure we quote it
                    if (arg.Contains(" ") || arg.Contains("\t"))
                    {
                        return $"\"{arg}\"";
                    }

                    return arg;
                })
                .ToArray();

            var dnxArgumentsAsString = string.Join(" ", dnxArguments);

            while (true)
            {
                var project = await WaitForValidProjectJsonAsync(projectFile, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                using (var currentRunCancellationSource = new CancellationTokenSource())
                using (var combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    currentRunCancellationSource.Token))
                {
                    var fileWatchingTask = WaitForProjectFileToChangeAsync(project, combinedCancellationSource.Token);
                    var dnxTask = WaitForDnxToExitAsync(dnxArgumentsAsString, workingDir, combinedCancellationSource.Token);

                    var tasksToWait = new Task[] { dnxTask, fileWatchingTask };

                    int finishedTaskIndex = Task.WaitAny(tasksToWait, cancellationToken);

                    // Regardless of the outcome, make sure everything is cancelled
                    // and wait for dnx to exit. We don't want orphan processes
                    currentRunCancellationSource.Cancel();
                    Task.WaitAll(tasksToWait);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (finishedTaskIndex == 0)
                    {
                        // This is the dnx task
                        var dnxExitCode = dnxTask.Result;

                        if (dnxExitCode == 0)
                        {
                            _logger.LogInformation($"dnx exit code: {dnxExitCode}");
                        }
                        else
                        {
                            _logger.LogError($"dnx exit code: {dnxExitCode}");
                        }

                        _logger.LogInformation("Waiting for a file to change before restarting dnx...");
                        // Now wait for a file to change before restarting dnx
                        await WaitForProjectFileToChangeAsync(project, cancellationToken);
                    }
                    else
                    {
                        // This is a file watcher task
                        string changedFile = fileWatchingTask.Result;
                        _logger.LogInformation($"File changed: {fileWatchingTask.Result}");
                    }
                }
            }
        }

        private async Task<string> WaitForProjectFileToChangeAsync(IProject project, CancellationToken cancellationToken)
        {
            using (var fileWatcher = _fileWatcherFactory(Path.GetDirectoryName(project.ProjectFile)))
            {
                AddProjectAndDependeciesToWatcher(project, fileWatcher);
                return await WatchForFileChangeAsync(fileWatcher, cancellationToken);
            }
        }

        private Task<int> WaitForDnxToExitAsync(string dnxArguments, string workingDir, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Running dnx with the following arguments: {dnxArguments}");

            var dnxWatcher = _processWatcherFactory();
            int dnxProcessId = dnxWatcher.Start("dnx", dnxArguments, workingDir);
            _logger.LogInformation($"dnx process id: {dnxProcessId}");

            return dnxWatcher.WaitForExitAsync(cancellationToken);
        }

        private async Task<IProject> WaitForValidProjectJsonAsync(string projectFile, CancellationToken cancellationToken)
        {
            IProject project = null;

            while (true)
            {
                string errors;
                if (_projectProvider.TryReadProject(projectFile, out project, out errors))
                {
                    return project;
                }

                _logger.LogError($"Error(s) reading project file '{projectFile}': ");
                _logger.LogError(errors);
                _logger.LogInformation("Fix the error to continue.");

                using (var fileWatcher = _fileWatcherFactory(Path.GetDirectoryName(projectFile)))
                {
                    fileWatcher.WatchFile(projectFile);
                    fileWatcher.WatchProject(projectFile);

                    await WatchForFileChangeAsync(fileWatcher, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    _logger.LogInformation($"File changed: {projectFile}");
                }
            }
        }

        private void AddProjectAndDependeciesToWatcher(string projectFile, IFileWatcher fileWatcher)
        {
            IProject project;
            string errors;

            if (_projectProvider.TryReadProject(projectFile, out project, out errors))
            {
                AddProjectAndDependeciesToWatcher(project, fileWatcher);
            }
        }

        private void AddProjectAndDependeciesToWatcher(IProject project, IFileWatcher fileWatcher)
        {
            foreach (var file in project.Files)
            {
                if (!string.IsNullOrEmpty(file))
                {
                    fileWatcher.WatchDirectory(
                        Path.GetDirectoryName(file),
                        Path.GetExtension(file));
                }
            }

            fileWatcher.WatchProject(project.ProjectFile);

            foreach (var projFile in project.ProjectDependencies)
            {
                AddProjectAndDependeciesToWatcher(projFile, fileWatcher);
            }
        }

        private async Task<string> WatchForFileChangeAsync(IFileWatcher fileWatcher, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>();

            cancellationToken.Register(() => tcs.TrySetResult(null));

            Action<string> callback = path => 
            {
                tcs.TrySetResult(path);
            };

            fileWatcher.OnChanged += callback;

            var changedPath = await tcs.Task;

            // Don't need to listen anymore
            fileWatcher.OnChanged -= callback;

            return changedPath;
        }

        public static DnxWatcher CreateDefault(ILoggerFactory loggerFactory)
        {
            return new DnxWatcher(
                fileWatcherFactory: root => new FileWatcher(root),
                processWatcherFactory: () => new ProcessWatcher(),
                projectProvider: new ProjectProvider(),
                loggerFactory: loggerFactory);
        }

    }
}
