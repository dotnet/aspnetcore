// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.Dnx.Watcher.Core;
using Microsoft.Framework.Logging;

namespace Microsoft.Dnx.Watcher
{
    public class Program
    {
        private const string DnxWatchArgumentSeparator = "--dnx-args";

        private readonly ILoggerFactory _loggerFactory;

        public Program(IRuntimeEnvironment runtimeEnvironment)
        {
            _loggerFactory = new LoggerFactory();

            var commandProvider = new CommandOutputProvider(runtimeEnvironment);
            _loggerFactory.AddProvider(commandProvider);
        }

        public int Main(string[] args)
        {
            using (CancellationTokenSource ctrlCTokenSource = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, ev) =>
                {
                    ctrlCTokenSource.Cancel();
                    ev.Cancel = false;
                };

                string[] watchArgs, dnxArgs;
                SeparateWatchArguments(args, out watchArgs, out dnxArgs);

                return MainInternal(watchArgs, dnxArgs, ctrlCTokenSource.Token);
            }
        }

        internal static void SeparateWatchArguments(string[] args, out string[] watchArgs, out string[] dnxArgs)
        {
            int argsIndex = -1;
            watchArgs = args.TakeWhile((arg, idx) =>
            {
                argsIndex = idx;
                return !string.Equals(arg, DnxWatchArgumentSeparator, StringComparison.OrdinalIgnoreCase);
            }).ToArray();

            dnxArgs = args.Skip(argsIndex + 1).ToArray();

            if (dnxArgs.Length == 0)
            {
                // If no explicit dnx arguments then all arguments get passed to dnx
                dnxArgs = watchArgs;
                watchArgs = new string[0];
            }
        }

        private int MainInternal(string[] watchArgs, string[] dnxArgs, CancellationToken cancellationToken)
        {
            var app = new CommandLineApplication();
            app.Name = "dnx-watch";
            app.FullName = "Microsoft .NET DNX File Watcher";

            app.HelpOption("-?|-h|--help");

            // Show help information if no subcommand/option was specified
            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 2;
            });

            var projectArg = app.Option(
                "--project <PATH>",
                "Path to the project.json file or the application folder. Defaults to the current folder if not provided. Will be passed to DNX.",
                CommandOptionType.SingleValue);

            var workingDirArg = app.Option(
                "--workingDir <DIR>",
                "The working directory for DNX. Defaults to the current directory.",
                CommandOptionType.SingleValue);

            // This option is here just to be displayed in help
            // it will not be parsed because it is removed before the code is executed
            app.Option(
                $"{DnxWatchArgumentSeparator} <ARGS>",
                "Marks the arguments that will be passed to DNX. Anything following this option is passed. If not specified, all the arguments are passed to DNX.",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var projectToRun = projectArg.HasValue() ?
                    projectArg.Value() :
                    Directory.GetCurrentDirectory();

                if (!projectToRun.EndsWith("project.json", StringComparison.Ordinal))
                {
                    projectToRun = Path.Combine(projectToRun, "project.json");
                }

                var workingDir = workingDirArg.HasValue() ?
                    workingDirArg.Value() :
                    Directory.GetCurrentDirectory();

                var watcher = DnxWatcher.CreateDefault(_loggerFactory);
                try
                {
                    watcher.WatchAsync(projectToRun, dnxArgs, workingDir, cancellationToken).Wait();
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Count != 1 || !(ex.InnerException is TaskCanceledException))
                    {
                        throw;
                    }
                }


                return 1;
            });

            return app.Execute(watchArgs);
        }
    }
}
