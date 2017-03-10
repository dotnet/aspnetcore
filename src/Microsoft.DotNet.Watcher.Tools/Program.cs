// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher
{
    public class Program
    {
        private readonly IConsole _console;
        private readonly string _workingDir;

        public Program(IConsole console, string workingDir)
        {
            Ensure.NotNull(console, nameof(console));
            Ensure.NotNullOrEmpty(workingDir, nameof(workingDir));

            _console = console;
            _workingDir = workingDir;
        }

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);
            return new Program(PhysicalConsole.Singleton, Directory.GetCurrentDirectory())
                .RunAsync(args)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<int> RunAsync(string[] args)
        {
            CommandLineOptions options;
            try
            {
                options = CommandLineOptions.Parse(args, _console);
            }
            catch (CommandParsingException ex)
            {
                CreateReporter(verbose: true, quiet: false, console: _console)
                    .Error(ex.Message);
                return 1;
            }

            if (options == null)
            {
                // invalid args syntax
                return 1;
            }

            if (options.IsHelp)
            {
                return 2;
            }

            var reporter = CreateReporter(options.IsVerbose, options.IsQuiet, _console);

            using (CancellationTokenSource ctrlCTokenSource = new CancellationTokenSource())
            {
                _console.CancelKeyPress += (sender, ev) =>
                {
                    if (!ctrlCTokenSource.IsCancellationRequested)
                    {
                        reporter.Output("Shutdown requested. Press Ctrl+C again to force exit.");
                        ev.Cancel = true;
                    }
                    else
                    {
                        ev.Cancel = false;
                    }
                    ctrlCTokenSource.Cancel();
                };

                try
                {
                    if (options.ListFiles)
                    {
                        return await ListFilesAsync(reporter,
                            options.Project,
                            ctrlCTokenSource.Token);
                    }
                    else
                    {

                        return await MainInternalAsync(reporter,
                            options.Project,
                            options.RemainingArguments,
                            ctrlCTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        // swallow when only exception is the CTRL+C forced an exit
                        return 0;
                    }

                    reporter.Error(ex.ToString());
                    reporter.Error("An unexpected error occurred");
                    return 1;
                }
            }
        }

        private async Task<int> MainInternalAsync(
            IReporter reporter,
            string project,
            ICollection<string> args,
            CancellationToken cancellationToken)
        {
            // TODO multiple projects should be easy enough to add here
            string projectFile;
            try
            {
                projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDir, project);
            }
            catch (FileNotFoundException ex)
            {
                reporter.Error(ex.Message);
                return 1;
            }

            var fileSetFactory = new MsBuildFileSetFactory(reporter, projectFile, waitOnError: true);
            var processInfo = new ProcessSpec
            {
                Executable = DotNetMuxer.MuxerPathOrDefault(),
                WorkingDirectory = Path.GetDirectoryName(projectFile),
                Arguments = args,
                EnvironmentVariables =
                {
                    ["DOTNET_WATCH"] = "1"
                },
            };

            await new DotNetWatcher(reporter)
                .WatchAsync(processInfo, fileSetFactory, cancellationToken);

            return 0;
        }

        private async Task<int> ListFilesAsync(
            IReporter reporter,
            string project,
            CancellationToken cancellationToken)
        {
            // TODO multiple projects should be easy enough to add here
            string projectFile;
            try
            {
                projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDir, project);
            }
            catch (FileNotFoundException ex)
            {
                reporter.Error(ex.Message);
                return 1;
            }

            var fileSetFactory = new MsBuildFileSetFactory(reporter, projectFile, waitOnError: false);
            var files = await fileSetFactory.CreateAsync(cancellationToken);

            if (files == null)
            {
                return 1;
            }

            foreach (var file in files)
            {
                _console.Out.WriteLine(file);
            }

            return 0;
        }

        private static IReporter CreateReporter(bool verbose, bool quiet, IConsole console)
            => new PrefixConsoleReporter(console, verbose || CliContext.IsGlobalVerbose(), quiet);
    }
}
