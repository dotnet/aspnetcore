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
    public class ConfigCommand : CommandWithStructuredInputBase<HttpState, ICoreParseResult>
    {
        protected override async Task ExecuteAsync(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (programState.BaseAddress == null)
            {
                shellState.ConsoleManager.Error.WriteLine("Must be connected to a server to query configuration".SetColor(programState.ErrorColor));
                return;
            }

            if (string.IsNullOrEmpty(programState.DiagnosticsState.DiagnosticsEndpoint))
            {
                shellState.ConsoleManager.Error.WriteLine("Diagnostics endpoint must be set to query configuration (see set diag)".SetColor(programState.ErrorColor));
                return;
            }

            string configUrl = programState.DiagnosticsState.DiagnosticItems.FirstOrDefault(x => x.DisplayName == "Configuration")?.Url;

            if (configUrl == null)
            {
                shellState.ConsoleManager.Error.WriteLine("Diagnostics endpoint does not expose configuration information".SetColor(programState.ErrorColor));
                return;
            }

            HttpResponseMessage response = await programState.Client.GetAsync(new Uri(programState.BaseAddress, configUrl), cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                shellState.ConsoleManager.Error.WriteLine("Unable to get configuration information from diagnostics endpoint".SetColor(programState.ErrorColor));
                return;
            }

            List<ConfigItem> configItems = await response.Content.ReadAsAsync<List<ConfigItem>>(cancellationToken).ConfigureAwait(false);

            foreach (ConfigItem item in configItems)
            {
                shellState.ConsoleManager.WriteLine($"{item.Key.Cyan()}: {item.Value}");
            }
        }

        public override CommandInputSpecification InputSpec { get; } = CommandInputSpecification.Create("config").Finish();

        protected override string GetHelpDetails(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult)
        {
            return "config - Gets configuration information for the site if connected to a diagnostics endpoint";
        }

        public override string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return "config - Gets configuration information for the site if connected to a diagnostics endpoint";
        }
    }
}
