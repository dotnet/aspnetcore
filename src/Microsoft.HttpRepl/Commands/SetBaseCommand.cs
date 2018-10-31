// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class SetBaseCommand : ICommand<HttpState, ICoreParseResult>
    {
        private const string Name = "set";
        private const string SubCommand = "base";

        public string Description => "Sets the base address to direct requests to.";

        public bool? CanHandle(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            return parseResult.Sections.Count > 1 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) && string.Equals(parseResult.Sections[1], SubCommand, StringComparison.OrdinalIgnoreCase)
                ? (bool?)true
                : null;
        }

        public async Task ExecuteAsync(IShellState shellState, HttpState state, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (parseResult.Sections.Count == 2)
            {
                state.BaseAddress = null;
            }
            else if (parseResult.Sections.Count != 3 || string.IsNullOrEmpty(parseResult.Sections[2]) || !Uri.TryCreate(EnsureTrailingSlash(parseResult.Sections[2]), UriKind.Absolute, out Uri serverUri))
            {
                shellState.ConsoleManager.Error.WriteLine("Must specify a server".SetColor(state.ErrorColor));
            }
            else
            {
                state.BaseAddress = serverUri;
                try
                {
                    await state.Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, serverUri)).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex.InnerException is SocketException se)
                {
                    shellState.ConsoleManager.Error.WriteLine($"Warning: HEAD request to the specified address was unsuccessful ({se.Message})".SetColor(state.WarningColor));
                }
                catch { }
            }

            if (state.BaseAddress == null || !Uri.TryCreate(state.BaseAddress, "swagger.json", out Uri result))
            {
                state.SwaggerStructure = null;
            }
            else
            {
                await SetSwaggerCommand.CreateDirectoryStructureForSwaggerEndpointAsync(shellState, state, result, cancellationToken).ConfigureAwait(false);
                if (state.SwaggerStructure != null)
                {
                    shellState.ConsoleManager.WriteLine("Using swagger metadata from " + result);
                }
                else
                {
                    if (state.BaseAddress == null || !Uri.TryCreate(state.BaseAddress, "swagger/v1/swagger.json", out result))
                    {
                        state.SwaggerStructure = null;
                    }
                    else
                    {
                        await SetSwaggerCommand.CreateDirectoryStructureForSwaggerEndpointAsync(shellState, state, result, cancellationToken).ConfigureAwait(false);
                        if (state.SwaggerStructure != null)
                        {
                            shellState.ConsoleManager.WriteLine("Using swagger metadata from " + result);
                        }
                    }
                }
            }
        }

        private string EnsureTrailingSlash(string v)
        {
            if (!v.EndsWith("/", StringComparison.Ordinal))
            {
                v += "/";
            }

            return v;
        }

        public string GetHelpDetails(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count > 1 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) && string.Equals(parseResult.Sections[1], SubCommand, StringComparison.OrdinalIgnoreCase))
            {
                var helpText = new StringBuilder();
                helpText.Append("Usage: ".Bold());
                helpText.AppendLine($"set base [uri]");
                helpText.AppendLine();
                helpText.AppendLine(Description);
                return helpText.ToString();
            }

            return null;
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

            return null;
        }
    }
}
