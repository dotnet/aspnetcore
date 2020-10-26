// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Extensions.ApiDescription.Tool.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal abstract class ProgramBase
    {
        private readonly IConsole _console;
        private readonly IReporter _reporter;

        public ProgramBase(IConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _reporter = new ConsoleReporter(_console, verbose: false, quiet: false);
        }

        protected static IConsole GetConsole()
        {
            var console = PhysicalConsole.Singleton;
            if (console.IsOutputRedirected)
            {
                Console.OutputEncoding = Encoding.UTF8;
            }

            return console;
        }

        protected int Run(string[] args, CommandBase command, bool throwOnUnexpectedArg)
        {
            try
            {
                // AllowArgumentSeparator and continueAfterUnexpectedArg are ignored when !throwOnUnexpectedArg _except_
                // AllowArgumentSeparator=true changes the help text (ignoring throwOnUnexpectedArg).
                var app = new CommandLineApplication(throwOnUnexpectedArg, continueAfterUnexpectedArg: true)
                {
                    AllowArgumentSeparator = !throwOnUnexpectedArg,
                    Error = _console.Error,
                    FullName = Resources.CommandFullName,
                    Name = Resources.CommandFullName,
                    Out = _console.Out,
                };

                command.Configure(app);

                return app.Execute(args);
            }
            catch (Exception ex)
            {
                if (ex is CommandException || ex is CommandParsingException)
                {
                    _reporter.WriteError(ex.Message);
                }
                else
                {
                    _reporter.WriteError(ex.ToString());
                }

                return 1;
            }
        }
    }
}
