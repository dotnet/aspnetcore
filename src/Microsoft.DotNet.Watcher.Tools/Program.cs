// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.DotNet.Watcher.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher
{
    public class Program
    {
        private const string AppArgumentSeparator = "--";

        private readonly ILoggerFactory _loggerFactory;

        public Program()
        {
            _loggerFactory = new LoggerFactory();

            var commandProvider = new CommandOutputProvider(PlatformServices.Default.Runtime);
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

                string[] watchArgs, appArgs;
                SeparateWatchArguments(args, out watchArgs, out appArgs);

                return new Program().MainInternal(watchArgs, appArgs, ctrlCTokenSource.Token);
            }
        }

        // The argument separation rules are: if no "--" is encountered, all arguments are passed to the app being watched.
        // Unless, the argument is "--help", in which case the help for the watcher is being invoked and everything else is discarded.
        // To pass arguments to both the watcher and the app use "--" as separator.
        // To pass "--help" to the app being watched, you must use "--": dotnet watch -- --help
        internal static void SeparateWatchArguments(string[] args, out string[] watchArgs, out string[] appArgs)
        {
            // Special case "--help"
            if (args.Length > 0 && (
                args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-?", StringComparison.OrdinalIgnoreCase)))
            {
                watchArgs = new string[] { args[0] };
                appArgs = new string[0];
                return;
            }

            int argsIndex = -1;
            watchArgs = args.TakeWhile((arg, idx) =>
            {
                argsIndex = idx;
                return !string.Equals(arg, AppArgumentSeparator, StringComparison.OrdinalIgnoreCase);
            }).ToArray();

            appArgs = args.Skip(argsIndex + 1).ToArray();

            if (appArgs.Length == 0)
            {
                // If no explicit watcher arguments then all arguments get passed to the app being watched
                appArgs = watchArgs;
                watchArgs = new string[0];
            }
        }

        private int MainInternal(string[] watchArgs, string[] appArgs, CancellationToken cancellationToken)
        {
            var app = new CommandLineApplication();
            app.Name = "dotnet-watch";
            app.FullName = "Microsoft dotnet File Watcher";

            app.HelpOption("-?|-h|--help");

            var commandArg = app.Option(
                "--command <COMMAND>",
                "Optional. The dotnet command to run. Default: 'run'.",
                CommandOptionType.SingleValue);

            var workingDirArg = app.Option(
                "--working-dir <DIR>",
                "Optional. The working directory. Default: project's directory.",
                CommandOptionType.SingleValue);

            var exitOnChangeArg = app.Option(
                "--exit-on-change",
                "Optional. The watcher will exit when a file change is detected instead of restarting the process. Default: not set.",
                CommandOptionType.NoValue);

           
            app.OnExecute(() =>
            {
                var projectToWatch = Path.Combine(Directory.GetCurrentDirectory(), ProjectModel.Project.FileName);

                var workingDir = workingDirArg.HasValue() ?
                    workingDirArg.Value() :
                    Path.GetDirectoryName(projectToWatch);

                var command = commandArg.HasValue() ?
                    commandArg.Value() :
                    "run";

                var watcher = DotNetWatcher.CreateDefault(_loggerFactory);
                watcher.ExitOnChange = exitOnChangeArg.HasValue();

                try
                {
                    watcher.WatchAsync(projectToWatch, command, appArgs, workingDir, cancellationToken).Wait();
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
