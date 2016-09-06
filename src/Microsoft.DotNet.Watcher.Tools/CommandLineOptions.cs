// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.DotNet.Watcher.Tools
{
    internal class CommandLineOptions
    {
        public bool IsHelp { get; private set; }
        public IList<string> RemainingArguments { get; private set; }
        public static CommandLineOptions Parse(string[] args, TextWriter consoleOutput)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet watch",
                FullName = "Microsoft DotNet File Watcher",
                Out = consoleOutput,
                AllowArgumentSeparator = true
            };

            app.HelpOption("-?|-h|--help");

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

            return new CommandLineOptions
            {
                RemainingArguments = app.RemainingArguments,
                IsHelp = app.IsShowingInformation
            };
        }
    }
}
