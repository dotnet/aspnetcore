// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher
{
    public class Program
    {
        private const string LoggerName = "DotNetWatcher";
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
            HandleDebugSwitch(ref args);
            return new Program(PhysicalConsole.Singleton, Directory.GetCurrentDirectory())
                .RunAsync(args)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<int> RunAsync(string[] args)
        {
            using (CancellationTokenSource ctrlCTokenSource = new CancellationTokenSource())
            {
                _console.CancelKeyPress += (sender, ev) =>
                {
                    if (!ctrlCTokenSource.IsCancellationRequested)
                    {
                        _console.Out.WriteLine($"[{LoggerName}] Shutdown requested. Press Ctrl+C again to force exit.");
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
                    return await MainInternalAsync(args, ctrlCTokenSource.Token);
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        // swallow when only exception is the CTRL+C forced an exit
                        return 0;
                    }

                    _console.Error.WriteLine(ex.ToString());
                    _console.Error.WriteLine($"[{LoggerName}] An unexpected error occurred".Bold().Red());
                    return 1;
                }
            }
        }

        private async Task<int> MainInternalAsync(string[] args, CancellationToken cancellationToken)
        {
            var options = CommandLineOptions.Parse(args, _console);
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
            string projectFile;
            try
            {
                projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDir, options.Project);
            }
            catch (FileNotFoundException ex)
            {
                _console.Error.WriteLine(ex.Message.Bold().Red());
                return 1;
            }

            var fileSetFactory = new MsBuildFileSetFactory(logger, projectFile);

            var processInfo = new ProcessSpec
            {
                Executable = DotNetMuxer.MuxerPathOrDefault(),
                WorkingDirectory = Path.GetDirectoryName(projectFile),
                Arguments = options.RemainingArguments
            };

            await new DotNetWatcher(logger)
                    .WatchAsync(processInfo, fileSetFactory, cancellationToken);

            return 0;
        }

        private LogLevel ResolveLogLevel(CommandLineOptions options)
        {
            if (options.IsQuiet)
            {
                return LogLevel.Warning;
            }

            bool globalVerbose;
            bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE"), out globalVerbose);

            if (options.IsVerbose // dotnet watch --verbose
                || globalVerbose) // dotnet --verbose watch
            {
                return LogLevel.Debug;
            }

            return LogLevel.Information;
        }

        [Conditional("DEBUG")]
        private static void HandleDebugSwitch(ref string[] args)
        {
            if (args.Length > 0 && string.Equals("--debug", args[0], StringComparison.OrdinalIgnoreCase))
            {
                args = args.Skip(1).ToArray();
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");
                Console.ReadLine();
            }
        }
    }
}
