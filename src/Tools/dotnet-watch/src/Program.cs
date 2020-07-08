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
    public class Program : IDisposable
    {
        private readonly IConsole _console;
        private readonly string _workingDirectory;
        private readonly CancellationTokenSource _cts;
        private IReporter _reporter;

        public Program(IConsole console, string workingDirectory)
        {
            Ensure.NotNull(console, nameof(console));
            Ensure.NotNullOrEmpty(workingDirectory, nameof(workingDirectory));

            _console = console;
            _workingDirectory = workingDirectory;
            _cts = new CancellationTokenSource();
            _console.CancelKeyPress += OnCancelKeyPress;
            _reporter = CreateReporter(verbose: true, quiet: false, console: _console);
        }

        public static async Task<int> Main(string[] args)
        {
            try
            {
                DebugHelper.HandleDebugSwitch(ref args);
                using (var program = new Program(PhysicalConsole.Singleton, Directory.GetCurrentDirectory()))
                {
                    return await program.RunAsync(args);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected error:");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
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
                _reporter.Error(ex.Message);
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

            // update reporter as configured by options
            _reporter = CreateReporter(options.IsVerbose, options.IsQuiet, _console);

            try
            {
                if (_cts.IsCancellationRequested)
                {
                    return 1;
                }

                if (options.ListFiles)
                {
                    return await ListFilesAsync(_reporter,
                        options.Project,
                        _cts.Token);
                }
                else
                {
                    return await MainInternalAsync(_reporter,
                        options.Project,
                        options.RemainingArguments,
                        _cts.Token);
                }
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    // swallow when only exception is the CTRL+C forced an exit
                    return 0;
                }

                _reporter.Error(ex.ToString());
                _reporter.Error("An unexpected error occurred");
                return 1;
            }
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            // suppress CTRL+C on the first press
            args.Cancel = !_cts.IsCancellationRequested;

            if (args.Cancel)
            {
                _reporter.Output("Shutdown requested. Press Ctrl+C again to force exit.");
            }

            _cts.Cancel();
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
                projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDirectory, project);
            }
            catch (FileNotFoundException ex)
            {
                reporter.Error(ex.Message);
                return 1;
            }

            var fileSetFactory = new MsBuildFileSetFactory(reporter,
                projectFile,
                waitOnError: true,
                trace: false);
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

            if (CommandLineOptions.IsPollingEnabled)
            {
                _reporter.Output("Polling file watcher is enabled");
            }

            await new DotNetWatcher(reporter, fileSetFactory)
                .WatchAsync(processInfo, cancellationToken);

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
                projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDirectory, project);
            }
            catch (FileNotFoundException ex)
            {
                reporter.Error(ex.Message);
                return 1;
            }

            var fileSetFactory = new MsBuildFileSetFactory(reporter,
                projectFile,
                waitOnError: false,
                trace: false);
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
            => new PrefixConsoleReporter("watch : ", console, verbose || CliContext.IsGlobalVerbose(), quiet);

        public void Dispose()
        {
            _console.CancelKeyPress -= OnCancelKeyPress;
            _cts.Dispose();
        }
    }
}
