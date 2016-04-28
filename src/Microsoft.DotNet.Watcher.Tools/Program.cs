// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Core;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class Program
    {
        private const string AppArgumentSeparator = "--";

        private readonly ILoggerFactory _loggerFactory;

        public Program()
        {
            _loggerFactory = new LoggerFactory();

            var commandProvider = new CommandOutputProvider();
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
            if (args.Length == 0)
            {
                watchArgs = new string[0];
                appArgs = new string[0];
                return;
            }

            // Special case "--help"
            if (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-?", StringComparison.OrdinalIgnoreCase))
            {
                watchArgs = new string[] { args[0] };
                appArgs = new string[0];
                return;
            }

            var separatorIndex = -1;
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], AppArgumentSeparator, StringComparison.OrdinalIgnoreCase))
                {
                    separatorIndex = i;
                    break;
                }
            }

            if (separatorIndex == -1)
            {
                watchArgs = new string[0];
                appArgs = args;
            }
            else
            {
                watchArgs = new string[separatorIndex];
                Array.Copy(args, 0, watchArgs, 0, separatorIndex);

                var appArgsLength = args.Length - separatorIndex - 1;
                appArgs = new string[appArgsLength];
                Array.Copy(args, separatorIndex + 1, appArgs, 0, appArgsLength);
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

                var command = commandArg.Value();
                if (!commandArg.HasValue())
                {
                    // The default command is "run". In this case we always assume the arguments are passed to the application being run.
                    // If you want a different behaviour for run you need to use --command and pass the full arguments
                    // Run is special because it requires a "--" before the arguments being passed to the application, 
                    // so the two command below are equivalent and resolve to "dotnet run -- --foo":
                    // 1. dotnet watch --foo
                    // 2. dotnet watch --command run -- -- --foo (yes, there are two "--")
                    if (appArgs.Length > 0)
                    {
                        var newAppArgs = new string[appArgs.Length + 1];
                        newAppArgs[0] = AppArgumentSeparator;
                        appArgs.CopyTo(newAppArgs, 1);
                        appArgs = newAppArgs;
                    }

                    command = "run";
                }

                var workingDir = workingDirArg.HasValue() ?
                    workingDirArg.Value() :
                    Path.GetDirectoryName(projectToWatch);

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
