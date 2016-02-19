// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Core.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher.Core
{
    public class DotNetWatcher
    {
        private readonly Func<IFileWatcher> _fileWatcherFactory;
        private readonly Func<IProcessWatcher> _processWatcherFactory;
        private readonly IProjectProvider _projectProvider;
        private readonly ILoggerFactory _loggerFactory;

        private readonly ILogger _logger;

        public bool ExitOnChange { get; set; }

        public DotNetWatcher(
            Func<IFileWatcher> fileWatcherFactory,
            Func<IProcessWatcher> processWatcherFactory,
            IProjectProvider projectProvider,
            ILoggerFactory loggerFactory)
        {
            _fileWatcherFactory = fileWatcherFactory;
            _processWatcherFactory = processWatcherFactory;
            _projectProvider = projectProvider;
            _loggerFactory = loggerFactory;

            _logger = _loggerFactory.CreateLogger(nameof(DotNetWatcher));
        }

        public async Task WatchAsync(string projectFile, string command, string[] dotnetArguments, string workingDir, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(projectFile))
            {
                throw new ArgumentNullException(nameof(projectFile));
            }
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException(nameof(command));
            }
            if (dotnetArguments == null)
            {
                throw new ArgumentNullException(nameof(dotnetArguments));
            }
            if (string.IsNullOrEmpty(workingDir))
            {
                throw new ArgumentNullException(nameof(workingDir));
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            if (dotnetArguments.Length > 0)
            {
                dotnetArguments = new string[] { command, "--" }
                    .Concat(dotnetArguments)
                    .ToArray();
            }
            else
            {
                dotnetArguments = new string[] { command };
            }

            dotnetArguments = dotnetArguments
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

            var dotnetArgumentsAsString = string.Join(" ", dotnetArguments);

            while (true)
            {
                await WaitForValidProjectJsonAsync(projectFile, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                using (var currentRunCancellationSource = new CancellationTokenSource())
                using (var combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    currentRunCancellationSource.Token))
                {
                    var fileWatchingTask = WaitForProjectFileToChangeAsync(projectFile, combinedCancellationSource.Token);
                    var dotnetTask = WaitForDotnetToExitAsync(dotnetArgumentsAsString, workingDir, combinedCancellationSource.Token);

                    var tasksToWait = new Task[] { dotnetTask, fileWatchingTask };

                    int finishedTaskIndex = Task.WaitAny(tasksToWait, cancellationToken);

                    // Regardless of the outcome, make sure everything is cancelled
                    // and wait for dotnet to exit. We don't want orphan processes
                    currentRunCancellationSource.Cancel();
                    Task.WaitAll(tasksToWait);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (finishedTaskIndex == 0)
                    {
                        // This is the dotnet task
                        var dotnetExitCode = dotnetTask.Result;

                        if (dotnetExitCode == 0)
                        {
                            _logger.LogInformation($"dotnet exit code: {dotnetExitCode}");
                        }
                        else
                        {
                            _logger.LogError($"dotnet exit code: {dotnetExitCode}");
                        }

                        if (ExitOnChange)
                        {
                            break;
                        }

                        _logger.LogInformation("Waiting for a file to change before restarting dotnet...");
                        // Now wait for a file to change before restarting dotnet
                        await WaitForProjectFileToChangeAsync(projectFile, cancellationToken);
                    }
                    else
                    {
                        // This is a file watcher task
                        string changedFile = fileWatchingTask.Result;
                        _logger.LogInformation($"File changed: {fileWatchingTask.Result}");

                        if (ExitOnChange)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private async Task<string> WaitForProjectFileToChangeAsync(string projectFile, CancellationToken cancellationToken)
        {
            using (var projectWatcher = CreateProjectWatcher(projectFile, watchProjectJsonOnly: false))
            {
                return await projectWatcher.WaitForChangeAsync(cancellationToken);
            }
        }

        private Task<int> WaitForDotnetToExitAsync(string dotnetArguments, string workingDir, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Running dotnet with the following arguments: {dotnetArguments}");

            var dotnetWatcher = _processWatcherFactory();
            int dotnetProcessId = dotnetWatcher.Start("dotnet", dotnetArguments, workingDir);
            _logger.LogInformation($"dotnet process id: {dotnetProcessId}");

            return dotnetWatcher.WaitForExitAsync(cancellationToken);
        }

        private async Task WaitForValidProjectJsonAsync(string projectFile, CancellationToken cancellationToken)
        {
            while (true)
            {
                IProject project;
                string errors;
                if (_projectProvider.TryReadProject(projectFile, out project, out errors))
                {
                    return;
                }

                _logger.LogError($"Error(s) reading project file '{projectFile}': ");
                _logger.LogError(errors);
                _logger.LogInformation("Fix the error to continue.");

                using (var projectWatcher = CreateProjectWatcher(projectFile, watchProjectJsonOnly: true))
                {
                    await projectWatcher.WaitForChangeAsync(cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    _logger.LogInformation($"File changed: {projectFile}");
                }
            }
        }

        private ProjectWatcher CreateProjectWatcher(string projectFile, bool watchProjectJsonOnly)
        {
            return new ProjectWatcher(projectFile, watchProjectJsonOnly, _fileWatcherFactory, _projectProvider);
        }

        public static DotNetWatcher CreateDefault(ILoggerFactory loggerFactory)
        {
            return new DotNetWatcher(
                fileWatcherFactory: () => new FileWatcher(),
                processWatcherFactory: () => new ProcessWatcher(),
                projectProvider: new ProjectProvider(),
                loggerFactory: loggerFactory);
        }
    }
}
