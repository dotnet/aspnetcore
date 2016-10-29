// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Watcher.Tools;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.DotNet.Watcher
{
    internal class CommandLineOptions
    {
        public string Project { get; private set; }
        public bool IsHelp { get; private set; }
        public bool IsQuiet { get; private set; }
        public bool IsVerbose { get; private set; }
        public IList<string> RemainingArguments { get; private set; }
        public static CommandLineOptions Parse(string[] args, TextWriter stdout, TextWriter stderr)
        {
            Ensure.NotNull(args, nameof(args));

            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet watch",
                FullName = "Microsoft DotNet File Watcher",
                Out = stdout,
                Error = stderr,
                AllowArgumentSeparator = true
            };

            app.HelpOption("-?|-h|--help");
            var optProjects = app.Option("-p|--project", "The project to watch",
                CommandOptionType.SingleValue); // TODO multiple shouldn't be too hard to support
            var optQuiet = app.Option("-q|--quiet", "Suppresses all output except warnings and errors",
                CommandOptionType.NoValue);
            var optVerbose = app.Option("-v|--verbose", "Show verbose output",
                CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                if (app.RemainingArguments.Count == 0)
                {
                    app.ShowHelp();
                }

                return 0;
            });

            if (app.Execute(args) != 0)
            {
                return null;
            }

            if (optQuiet.HasValue() && optVerbose.HasValue())
            {
                stderr.WriteLine(Resources.Error_QuietAndVerboseSpecified.Bold().Red());
                return null;
            }

            return new CommandLineOptions
            {
                Project = optProjects.Value(),
                IsQuiet = optQuiet.HasValue(),
                IsVerbose = optVerbose.HasValue(),
                RemainingArguments = app.RemainingArguments,
                IsHelp = app.IsShowingInformation
            };
        }
    }
}
