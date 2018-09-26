// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.HttpRepl.Suggestions;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class SetHeaderCommand : ICommand<HttpState, ICoreParseResult>
    {
        private static readonly string Name = "set";
        private static readonly string SubCommand = "header";

        public string Description => "set header {name} [value] - Sets or clears a header";

        public bool? CanHandle(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            return parseResult.Sections.Count > 2 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) && string.Equals(parseResult.Sections[1], SubCommand, StringComparison.OrdinalIgnoreCase)
                ? (bool?)true
                : null;
        }

        public Task ExecuteAsync(IShellState shellState, HttpState programState, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (parseResult.Sections.Count == 3)
            {
                programState.Headers.Remove(parseResult.Sections[2]);
            }
            else
            {
                programState.Headers[parseResult.Sections[2]] = parseResult.Sections.Skip(3).ToList();
            }

            return Task.CompletedTask;
        }

        public string GetHelpDetails(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            var helpText = new StringBuilder();
            helpText.Append("Usage: ".Bold());
            helpText.AppendLine("set header {name} [value]");
            helpText.AppendLine();
            helpText.AppendLine("Sets or clears a header. When [value] is empty the header is cleared.");
            return Description;
        }

        public string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return Description;
        }

        public IEnumerable<string> Suggest(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count == 0)
            {
                return new[] { Name };
            }

            if (parseResult.Sections.Count > 0 && parseResult.SelectedSection == 0 && Name.StartsWith(parseResult.Sections[0].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase))
            {
                return new[] { Name };
            }

            if (string.Equals(Name, parseResult.Sections[0], StringComparison.OrdinalIgnoreCase) && parseResult.SelectedSection == 1 && (parseResult.Sections.Count < 2 || SubCommand.StartsWith(parseResult.Sections[1].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase)))
            {
                return new[] { SubCommand };
            }

            if (parseResult.Sections.Count > 2
                && string.Equals(Name, parseResult.Sections[0], StringComparison.OrdinalIgnoreCase)
                && string.Equals(SubCommand, parseResult.Sections[1], StringComparison.OrdinalIgnoreCase) && parseResult.SelectedSection == 2)
            {
                string prefix = parseResult.Sections[2].Substring(0, parseResult.CaretPositionWithinSelectedSection);
                return HeaderCompletion.GetCompletions(null, prefix);
            }

            if (parseResult.Sections.Count > 3
                && string.Equals(Name, parseResult.Sections[0], StringComparison.OrdinalIgnoreCase)
                && string.Equals(SubCommand, parseResult.Sections[1], StringComparison.OrdinalIgnoreCase) && parseResult.SelectedSection == 3)
            {
                string prefix = parseResult.Sections[3].Substring(0, parseResult.CaretPositionWithinSelectedSection);
                return HeaderCompletion.GetValueCompletions(null, string.Empty, parseResult.Sections[2], prefix, programState);
            }

            return null;
        }
    }
}
