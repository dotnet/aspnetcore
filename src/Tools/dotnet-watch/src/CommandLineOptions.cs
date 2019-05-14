// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.DotNet.Watcher.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher
{
    internal class CommandLineOptions
    {
        public string Project { get; private set; }
        public bool IsHelp { get; private set; }
        public bool IsQuiet { get; private set; }
        public bool IsVerbose { get; private set; }
        public IList<string> RemainingArguments { get; private set; }
        public bool ListFiles { get; private set; }

        public static bool IsPollingEnabled
        {
            get
            {
                var envVar = Environment.GetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER");
                return envVar != null &&
                    (envVar.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                     envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
            }
        }

        public static CommandLineOptions Parse(string[] args, IConsole console)
        {
            Ensure.NotNull(args, nameof(args));
            Ensure.NotNull(console, nameof(console));

            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet watch",
                FullName = "Microsoft DotNet File Watcher",
                Out = console.Out,
                Error = console.Error,
                AllowArgumentSeparator = true,
                ExtendedHelpText = @"
Environment variables:

  DOTNET_USE_POLLING_FILE_WATCHER
  When set to '1' or 'true', dotnet-watch will poll the file system for
  changes. This is required for some file systems, such as network shares,
  Docker mounted volumes, and other virtual file systems.

  DOTNET_WATCH
  dotnet-watch sets this variable to '1' on all child processes launched.

  DOTNET_WATCH_ITERATION
  dotnet-watch sets this variable to '1' and increments by one each time
  a file is changed and the command is restarted.

Remarks:
  The special option '--' is used to delimit the end of the options and
  the beginning of arguments that will be passed to the child dotnet process.
  Its use is optional. When the special option '--' is not used,
  dotnet-watch will use the first unrecognized argument as the beginning
  of all arguments passed into the child dotnet process.

  For example: dotnet watch -- --verbose run

  Even though '--verbose' is an option dotnet-watch supports, the use of '--'
  indicates that '--verbose' should be treated instead as an argument for
  dotnet-run.

Examples:
  dotnet watch run
  dotnet watch test
"
            };

            app.HelpOption("-?|-h|--help");
            // TODO multiple shouldn't be too hard to support
            var optProjects = app.Option("-p|--project <PROJECT>", "The project to watch",
                CommandOptionType.SingleValue);

            var optQuiet = app.Option("-q|--quiet", "Suppresses all output except warnings and errors",
                CommandOptionType.NoValue);
            var optVerbose = app.VerboseOption();

            var optList = app.Option("--list", "Lists all discovered files without starting the watcher",
                CommandOptionType.NoValue);

            app.VersionOptionFromAssemblyAttributes(typeof(Program).GetTypeInfo().Assembly);

            if (app.Execute(args) != 0)
            {
                return null;
            }

            if (optQuiet.HasValue() && optVerbose.HasValue())
            {
                throw new CommandParsingException(app, Resources.Error_QuietAndVerboseSpecified);
            }

            if (app.RemainingArguments.Count == 0
                && !app.IsShowingInformation
                && !optList.HasValue())
            {
                app.ShowHelp();
            }

            return new CommandLineOptions
            {
                Project = optProjects.Value(),
                IsQuiet = optQuiet.HasValue(),
                IsVerbose = optVerbose.HasValue(),
                RemainingArguments = app.RemainingArguments,
                IsHelp = app.IsShowingInformation,
                ListFiles = optList.HasValue(),
            };
        }
    }
}
