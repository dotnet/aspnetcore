// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class ClearCommand : ICommand<object, ICoreParseResult>
    {
        private static readonly string Name = "clear";
        private static readonly string AlternateName = "cls";

        public bool? CanHandle(IShellState shellState, object programState, ICoreParseResult parseResult)
        {
            return parseResult.Sections.Count == 1 && (string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) || string.Equals(parseResult.Sections[0], AlternateName, StringComparison.OrdinalIgnoreCase))
                ? (bool?) true
                : null;
        }

        public Task ExecuteAsync(IShellState shellState, object programState, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            shellState.ConsoleManager.Clear();
            shellState.CommandDispatcher.OnReady(shellState);
            return Task.CompletedTask;
        }

        public string GetHelpDetails(IShellState shellState, object programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count == 1 && (string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) || string.Equals(parseResult.Sections[0], AlternateName, StringComparison.OrdinalIgnoreCase)))
            {
                return "Clears the shell";
            }

            return null;
        }

        public string GetHelpSummary(IShellState shellState, object programState)
        {
            return "clear - Clears the shell";
        }

        public IEnumerable<string> Suggest(IShellState shellState, object programState, ICoreParseResult parseResult)
        {
            if (parseResult.SelectedSection == 0 && 
                (string.IsNullOrEmpty(parseResult.Sections[parseResult.SelectedSection]) || Name.StartsWith(parseResult.Sections[0].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase)))
            {
                return new[] { Name };
            }

            if (parseResult.SelectedSection == 0 &&
                (string.IsNullOrEmpty(parseResult.Sections[parseResult.SelectedSection]) || AlternateName.StartsWith(parseResult.Sections[0].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase)))
            {
                return new[] { Name };
            }

            return null;
        }
    }
}
