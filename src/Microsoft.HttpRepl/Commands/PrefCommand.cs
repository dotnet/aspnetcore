// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.HttpRepl.Preferences;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class PrefCommand : CommandWithStructuredInputBase<HttpState, ICoreParseResult>
    {
        private readonly HashSet<string> _allowedSubcommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"get", "set"};

        public override string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return "pref [get/set] {setting} [{value}] - Allows viewing or changing preferences";
        }

        protected override bool CanHandle(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput)
        {
            if (commandInput.Arguments.Count == 0 || !_allowedSubcommands.Contains(commandInput.Arguments[0]?.Text))
            {
                shellState.ConsoleManager.Error.WriteLine("Whether get or set settings must be specified");
                return false;
            }

            if (!string.Equals("get", commandInput.Arguments[0].Text) && (commandInput.Arguments.Count < 2 || string.IsNullOrEmpty(commandInput.Arguments[1]?.Text)))
            {
                shellState.ConsoleManager.Error.WriteLine("The preference to set must be specified");
                return false;
            }

            return true;
        }

        protected override string GetHelpDetails(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult)
        {
            if (commandInput.Arguments.Count == 0 || !_allowedSubcommands.Contains(commandInput.Arguments[0]?.Text))
            {
                return "pref [get/set] {setting} [{value}] - Get or sets a preference to a particular value";
            }

            if (string.Equals(commandInput.Arguments[0].Text, "get", StringComparison.OrdinalIgnoreCase))
            {
                return "pref get [{setting}] - Gets the value of the specified preference or lists all preferences if no preference is specified";
            }
            else
            {
                return "pref set {setting} [{value}] - Sets (or clears if value is not specified) the value of the specified preference";
            }
        }

        protected override Task ExecuteAsync(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (string.Equals(commandInput.Arguments[0].Text, "get", StringComparison.OrdinalIgnoreCase))
            {
                return GetSetting(shellState, programState, commandInput);
            }

            return SetSetting(shellState, programState, commandInput);
        }

        private static Task SetSetting(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput)
        {
            string prefName = commandInput.Arguments[1].Text;
            string prefValue = commandInput.Arguments.Count > 2 ? commandInput.Arguments[2]?.Text : null;

            if (string.IsNullOrEmpty(prefValue))
            {
                if (!programState.DefaultPreferences.TryGetValue(prefName, out string defaultValue))
                {
                    programState.Preferences.Remove(prefName);
                }
                else
                {
                    programState.Preferences[prefName] = defaultValue;
                }
            }
            else
            {
                programState.Preferences[prefName] = prefValue;
            }

            if (!programState.SavePreferences())
            {
                shellState.ConsoleManager.Error.WriteLine("Error saving preferences".Bold().Red());
            }

            return Task.CompletedTask;
        }

        private static Task GetSetting(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput)
        {
            string preferenceName = commandInput.Arguments.Count > 1 ? commandInput.Arguments[1]?.Text : null;
            
            //If there's a particular setting to get the value of
            if (!string.IsNullOrEmpty(preferenceName))
            {
                if (programState.Preferences.TryGetValue(preferenceName, out string value))
                {
                    shellState.ConsoleManager.WriteLine("Configured value: " + value);
                }
                else
                {
                    shellState.ConsoleManager.Error.WriteLine((commandInput.Arguments[1].Text + " does not have a configured value").Bold().Red());
                }
            }
            else
            {
                foreach (KeyValuePair<string, string> entry in programState.Preferences.OrderBy(x => x.Key))
                {
                    shellState.ConsoleManager.WriteLine($"{entry.Key}={entry.Value}");
                }
            }

            return Task.CompletedTask;
        }

        protected override CommandInputSpecification InputSpec { get; } = CommandInputSpecification.Create("pref")
            .MinimumArgCount(1)
            .MaximumArgCount(3)
            .Finish();


        protected override IEnumerable<string> GetArgumentSuggestionsForText(IShellState shellState, HttpState programState, ICoreParseResult parseResult, DefaultCommandInput<ICoreParseResult> commandInput, string normalCompletionString)
        {
            if (parseResult.SelectedSection == 1)
            {
                return _allowedSubcommands.Where(x => x.StartsWith(normalCompletionString, StringComparison.OrdinalIgnoreCase));
            }

            if (parseResult.SelectedSection == 2)
            {
                string prefix = parseResult.Sections.Count > 2 ? normalCompletionString : string.Empty;
                List<string> matchingProperties = new List<string>();

                foreach (string val in WellKnownPreference.Catalog.Names)
                {
                    if (val.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingProperties.Add(val);
                    }
                }

                return matchingProperties;
            }

            if (parseResult.SelectedSection == 3
                && parseResult.Sections[2].StartsWith("colors.", StringComparison.OrdinalIgnoreCase))
            {
                string prefix = parseResult.Sections.Count > 3 ? normalCompletionString : string.Empty;
                List<string> matchingProperties = new List<string>();

                foreach (string val in Enum.GetNames(typeof(AllowedColors)))
                {
                    if (val.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingProperties.Add(val);
                    }
                }

                return matchingProperties;
            }

            return null;
        }
    }
}
