// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher
{
    public class Program
    {
        private const string LoggerName = "DotNetWatcher";
        private readonly CancellationToken _cancellationToken;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;
        private readonly string _workingDir;

        public Program(TextWriter consoleOutput, TextWriter consoleError, string workingDir, CancellationToken cancellationToken)
        {
            Ensure.NotNull(consoleOutput, nameof(consoleOutput));
            Ensure.NotNull(consoleError, nameof(consoleError));
            Ensure.NotNullOrEmpty(workingDir, nameof(workingDir));

            _cancellationToken = cancellationToken;
            _stdout = consoleOutput;
            _stderr = consoleError;
            _workingDir = workingDir;
        }

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            using (CancellationTokenSource ctrlCTokenSource = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, ev) =>
                {
                    if (!ctrlCTokenSource.IsCancellationRequested)
                    {
                        Console.WriteLine($"[{LoggerName}] Shutdown requested. Press CTRL+C again to force exit.");
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
                    return new Program(Console.Out, Console.Error, Directory.GetCurrentDirectory(), ctrlCTokenSource.Token)
                        .MainInternalAsync(args)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        // swallow when only exception is the CTRL+C forced an exit
                        return 0;
                    }

                    Console.Error.WriteLine(ex.ToString());
                    Console.Error.WriteLine($"[{LoggerName}] An unexpected error occurred".Bold().Red());
                    return 1;
                }
            }
        }

        private async Task<int> MainInternalAsync(string[] args)
        {
            var options = CommandLineOptions.Parse(args, _stdout, _stdout);
            if (options == null)
            {
                // invalid args syntax
                return 1;
            }

            if (options.IsHelp)
            {
                return 2;
            }

            var loggerFactory = new LoggerFactory();
            var commandProvider = new CommandOutputProvider
            {
                LogLevel = ResolveLogLevel(options)
            };
            loggerFactory.AddProvider(commandProvider);
            var logger = loggerFactory.CreateLogger(LoggerName);

            // TODO multiple projects should be easy enough to add here
            var projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDir, options.Project);
            var fileSetFactory = new MsBuildFileSetFactory(logger, projectFile);

            var processInfo = new ProcessSpec
            {
                Executable = new Muxer().MuxerPath,
                WorkingDirectory = Path.GetDirectoryName(projectFile),
                Arguments = options.RemainingArguments
            };

            await new DotNetWatcher(logger)
                    .WatchAsync(processInfo, fileSetFactory, _cancellationToken);

            return 0;
        }

        private LogLevel ResolveLogLevel(CommandLineOptions options)
        {
            if (options.IsQuiet)
            {
                return LogLevel.Warning;
            }

            bool globalVerbose;
            bool.TryParse(Environment.GetEnvironmentVariable(CommandContext.Variables.Verbose), out globalVerbose);

            if (options.IsVerbose // dotnet watch --verbose
                || globalVerbose) // dotnet --verbose watch
            {
                return LogLevel.Debug;
            }

            return LogLevel.Information;
        }
    }
}
