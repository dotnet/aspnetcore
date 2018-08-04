// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class UICommand : ICommand<HttpState, ICoreParseResult>
    {
        private static readonly string Name = "ui";

        public bool? CanHandle(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            return parseResult.Sections.Count == 1 && string.Equals(parseResult.Sections[0], Name)
                ? (bool?)true
                : null;
        }

        public Task ExecuteAsync(IShellState shellState, HttpState programState, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (programState.BaseAddress == null)
            {
                shellState.ConsoleManager.Error.WriteLine("Must be connected to a server to launch Swagger UI".SetColor(programState.ErrorColor));
                return Task.CompletedTask;
            }

            Uri uri = new Uri(programState.BaseAddress, "swagger");
            string agent = "cmd";
            string agentParam = $"/c start {uri.AbsoluteUri}";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                agent = "open";
                agentParam = uri.AbsoluteUri;
            }
            else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                agent = "xdg-open";
                agentParam = uri.AbsoluteUri;
            }

            Process.Start(new ProcessStartInfo(agent, agentParam) { CreateNoWindow = true });
            return Task.CompletedTask;
        }

        public string GetHelpDetails(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count == 1 && string.Equals(parseResult.Sections[0], Name))
            {
                return "ui - Launches the Swagger UI page (if available) in the default browser";
            }

            return null;
        }

        public string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return "ui - Launches the Swagger UI page (if available) in the default browser";
        }

        public IEnumerable<string> Suggest(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.SelectedSection == 0 &&
                (string.IsNullOrEmpty(parseResult.Sections[parseResult.SelectedSection]) || Name.StartsWith(parseResult.Sections[0].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase)))
            {
                return new[] { Name };
            }

            return null;
        }
    }
}
