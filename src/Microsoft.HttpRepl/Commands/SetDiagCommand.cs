// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.HttpRepl.Diagnostics;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class SetDiagCommand : ICommand<HttpState, ICoreParseResult>
    {
        private static readonly string Name = "set";
        private static readonly string SubCommand = "diag";

        public string Description => "Sets the diagnostics path to direct requests to.";

        public bool? CanHandle(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            return parseResult.Sections.Count > 1 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) && string.Equals(parseResult.Sections[1], SubCommand, StringComparison.OrdinalIgnoreCase)
                ? (bool?)true
                : null;
        }

        public async Task ExecuteAsync(IShellState shellState, HttpState programState, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (parseResult.Sections.Count == 2)
            {
                programState.DiagnosticsState.DiagnosticsEndpoint = null;
                programState.DiagnosticsState.DiagnosticItems = null;
                programState.DiagnosticsState.DiagEndpointsStructure = null;
                return;
            }

            if (parseResult.Sections.Count != 3 || string.IsNullOrEmpty(parseResult.Sections[2]) || !Uri.TryCreate(parseResult.Sections[2], UriKind.Relative, out Uri _))
            {
                shellState.ConsoleManager.Error.WriteLine("Must specify a relative path".SetColor(programState.ErrorColor));
            }
            else
            {
                programState.DiagnosticsState.DiagnosticsEndpoint = parseResult.Sections[2];
                HttpResponseMessage response = await programState.Client.GetAsync(new Uri(programState.BaseAddress, programState.DiagnosticsState.DiagnosticsEndpoint), cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    shellState.ConsoleManager.Error.WriteLine("Unable to access diagnostics endpoint".SetColor(programState.ErrorColor));
                    programState.DiagnosticsState.DiagnosticsEndpoint = null;
                    programState.DiagnosticsState.DiagnosticItems = null;
                }
                else
                {
                    programState.DiagnosticsState.DiagnosticItems = (await response.Content.ReadAsAsync<Dictionary<string, DiagItem>>(cancellationToken).ConfigureAwait(false))?.Select(x => x.Value).ToList();

                    DiagItem endpointsItem = programState.DiagnosticsState.DiagnosticItems?.FirstOrDefault(x => string.Equals(x.DisplayName, "Endpoints", StringComparison.OrdinalIgnoreCase));

                    if (endpointsItem != null)
                    {
                        HttpResponseMessage endpointsResponse = await programState.Client.GetAsync(new Uri(programState.BaseAddress, endpointsItem.Url), cancellationToken).ConfigureAwait(false);

                        if (!endpointsResponse.IsSuccessStatusCode)
                        {
                            shellState.ConsoleManager.Error.WriteLine("Unable to get endpoints information from diagnostics endpoint".SetColor(programState.ErrorColor));
                            return;
                        }

                        List<DiagEndpoint> endpoints = await endpointsResponse.Content.ReadAsAsync<List<DiagEndpoint>>(cancellationToken).ConfigureAwait(false);
                        DirectoryStructure structure = new DirectoryStructure(null);

                        foreach (DiagEndpoint endpoint in endpoints)
                        {
                            if (endpoint.Url.StartsWith(endpointsItem.Url, StringComparison.OrdinalIgnoreCase)
                                || endpoint.Url.StartsWith("/graphql", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            FillDirectoryInfo(structure, endpoint.Url);
                        }

                        programState.DiagnosticsState.DiagEndpointsStructure = structure;
                    }
                }
            }
        }

        private static void FillDirectoryInfo(DirectoryStructure parent, string endpoint)
        {
            string[] parts = endpoint.Split('/');

            foreach (string part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    parent = parent.DeclareDirectory(part);
                }
            }
        }

        public string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return Description;
        }

        public string GetHelpDetails(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count > 1 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) && string.Equals(parseResult.Sections[1], SubCommand, StringComparison.OrdinalIgnoreCase))
            {
                return Description;
            }

            return null;
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

            return null;
        }
    }
}
