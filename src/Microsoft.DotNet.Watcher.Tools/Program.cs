// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Watcher
{
    public class Program
    {
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
        private readonly CancellationToken _cancellationToken;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;

        public Program(TextWriter consoleOutput, TextWriter consoleError, CancellationToken cancellationToken)
        {
            if (consoleOutput == null)
            {
                throw new ArgumentNullException(nameof(consoleOutput));
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            _cancellationToken = cancellationToken;
            _stdout = consoleOutput;
            _stderr = consoleError;
        }

        public static int Main(string[] args)
        {
            using (CancellationTokenSource ctrlCTokenSource = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, ev) =>
                {
                    ctrlCTokenSource.Cancel();
                    ev.Cancel = false;
                };

                int exitCode;
                try
                {
                    exitCode = new Program(Console.Out, Console.Error, ctrlCTokenSource.Token)
                        .MainInternalAsync(args)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (TaskCanceledException)
                {
                    // swallow when only exception is the CTRL+C exit cancellation task
                    exitCode = 0;
                }
                return exitCode;
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

            var commandProvider = new CommandOutputProvider
            {
                LogLevel = ResolveLogLevel(options)
            };
            _loggerFactory.AddProvider(commandProvider);

            var projectToWatch = Path.Combine(Directory.GetCurrentDirectory(), ProjectModel.Project.FileName);

            await DotNetWatcher
                    .CreateDefault(_loggerFactory)
                    .WatchAsync(projectToWatch, options.RemainingArguments, _cancellationToken);

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
