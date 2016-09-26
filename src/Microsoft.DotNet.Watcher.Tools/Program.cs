// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher
{
    public class Program
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly CancellationToken _cancellationToken;
        private readonly TextWriter _out;

        public Program(TextWriter consoleOutput, CancellationToken cancellationToken)
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
            _out = consoleOutput;

            _loggerFactory = new LoggerFactory();

            var logVar = Environment.GetEnvironmentVariable("DOTNET_WATCH_LOG_LEVEL");

            LogLevel logLevel;
            if (string.IsNullOrEmpty(logVar) || !Enum.TryParse<LogLevel>(logVar, out logLevel))
            {
                logLevel = LogLevel.Information;
            }

            var commandProvider = new CommandOutputProvider()
            {
                LogLevel = logLevel
            };
            _loggerFactory.AddProvider(commandProvider);
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
                    exitCode = new Program(Console.Out, ctrlCTokenSource.Token)
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
            var options = CommandLineOptions.Parse(args, _out);
            if (options == null)
            {
                // invalid args syntax
                return 1;
            }

            if (options.IsHelp)
            {
                return 2;
            }

            var projectToWatch = Path.Combine(Directory.GetCurrentDirectory(), ProjectModel.Project.FileName);

            await DotNetWatcher
                    .CreateDefault(_loggerFactory)
                    .WatchAsync(projectToWatch, options.RemainingArguments, _cancellationToken);

            return 0;
        }
    }
}
