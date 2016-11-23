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
                    return await MainInternalAsync(reporter, options.Project, options.RemainingArguments, ctrlCTokenSource.Token);
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

            var fileSetFactory = new MsBuildFileSetFactory(reporter, projectFile);

            var processInfo = new ProcessSpec
            {
                Executable = DotNetMuxer.MuxerPathOrDefault(),
                WorkingDirectory = Path.GetDirectoryName(projectFile),
                Arguments = args
            };

            await new DotNetWatcher(reporter)
                .WatchAsync(processInfo, fileSetFactory, cancellationToken);

            return 0;
        }

        private static IReporter CreateReporter(bool verbose, bool quiet, IConsole console)
        {
            const string prefix = "watch : ";
            var colorPrefix = new ColorFormatter(ConsoleColor.DarkGray).Format(prefix);

            return new ReporterBuilder()
                .WithConsole(console)
                .Verbose(f =>
                {
                    if (console.IsOutputRedirected)
                    {
                        f.WithPrefix(prefix);
                    }
                    else
                    {
                        f.WithColor(ConsoleColor.DarkGray).WithPrefix(colorPrefix);
                    }

                    f.When(() => verbose || CliContext.IsGlobalVerbose());
                })
                .Output(f => f
                    .WithPrefix(console.IsOutputRedirected ? prefix : colorPrefix)
                    .When(() => !quiet))
                .Warn(f =>
                {
                    if (console.IsOutputRedirected)
                    {
                        f.WithPrefix(prefix);
                    }
                    else
                    {
                        f.WithColor(ConsoleColor.Yellow).WithPrefix(colorPrefix);
                    }
                })
                .Error(f =>
                {
                    if (console.IsOutputRedirected)
                    {
                        f.WithPrefix(prefix);
                    }
                    else
                    {
                        f.WithColor(ConsoleColor.Red).WithPrefix(colorPrefix);
                    }
                })
                .Build();
        }
    }
}