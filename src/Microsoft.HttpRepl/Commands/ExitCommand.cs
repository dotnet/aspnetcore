// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class ExitCommand : CommandWithStructuredInputBase<object, ICoreParseResult>
    {
        protected override Task ExecuteAsync(IShellState shellState, object programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            shellState.IsExiting = true;
            return Task.CompletedTask;
        }

        public override CommandInputSpecification InputSpec { get; } = CommandInputSpecification.Create("exit").ExactArgCount(0).Finish();

        protected override string GetHelpDetails(IShellState shellState, object programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult)
        {
            var helpText = new StringBuilder();
            helpText.Append("Usage: ".Bold());
            helpText.AppendLine($"exit");
            helpText.AppendLine();
            helpText.AppendLine($"Exits the shell");
            return helpText.ToString();
        }

        public override string GetHelpSummary(IShellState shellState, object programState)
        {
            return "exit - Exits the shell";
        }
    }
}
