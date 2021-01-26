// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal abstract class CommandBase
    {
        private readonly IConsole _console;

        public bool IsQuiet { get; private set; }

        public bool IsVerbose { get; private set; }

        protected IReporter Reporter { get; private set; }

        protected CommandBase(IConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            Reporter = new ConsoleReporter(_console);
        }

        public virtual void Configure(CommandLineApplication command)
        {
            var prefixOutput = command.Option("--prefix-output", Resources.PrefixDescription);
            var quiet = command.Option("-q|--quiet", Resources.QuietDescription);
            var verbose = command.VerboseOption();

            command.OnExecute(
                () =>
                {
                    IsQuiet = quiet.HasValue();
                    IsVerbose = verbose.HasValue() || CliContext.IsGlobalVerbose();
                    ReporterExtensions.PrefixOutput = prefixOutput.HasValue();

                    // Update the reporter now that we know the option values.
                    Reporter = new ConsoleReporter(_console, IsVerbose, IsQuiet);

                    Validate();

                    return Execute();
                });
        }

        protected virtual void Validate()
        {
            if (IsQuiet && IsVerbose)
            {
                throw new CommandException(Resources.QuietAndVerboseSpecified);
            }
        }

        protected virtual int Execute() => 0;
    }
}
