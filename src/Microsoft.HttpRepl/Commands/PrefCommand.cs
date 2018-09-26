// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var helpText = new StringBuilder();
            helpText.Append("Usage: ".Bold());

            if (commandInput.Arguments.Count == 0 || !_allowedSubcommands.Contains(commandInput.Arguments[0]?.Text))
            {
                helpText.AppendLine("pref [get/set] {setting} [{value}] - Get or sets a preference to a particular value");
            }
            else if (string.Equals(commandInput.Arguments[0].Text, "get", StringComparison.OrdinalIgnoreCase))
            {
                helpText.AppendLine("pref get [{setting}] - Gets the value of the specified preference or lists all preferences if no preference is specified");
            }
            else
            {
                helpText.AppendLine("pref set {setting} [{value}] - Sets (or clears if value is not specified) the value of the specified preference");
            }

            helpText.AppendLine();
            helpText.AppendLine("Current Default Preferences:");
            foreach (var pref in programState.DefaultPreferences)
            {
                var val = pref.Value;
                if (pref.Key.Contains("colors"))
                {
                    val = GetColor(val);
                }
                helpText.AppendLine($"{pref.Key,-50}{val}");
            }
            helpText.AppendLine();
            helpText.AppendLine("Current Preferences:");
            foreach (var pref in programState.Preferences)
            {
                var val = pref.Value;
                if (pref.Key.Contains("colors"))
                {
                    val = GetColor(val);
                }
                helpText.AppendLine($"{pref.Key,-50}{val}");
            }

            return helpText.ToString();
        }

        private static string GetColor(string value)
        {
            if (value.Contains("Bold"))
            {
                value = value.Bold();
            }

            if (value.Contains("Yellow"))
            {
                value = value.Yellow();
            }

            if (value.Contains("Cyan"))
            {
                value = value.Cyan();
            }

            if (value.Contains("Magenta"))
            {
                value = value.Magenta();
            }

            if (value.Contains("Green"))
            {
                value = value.Green();
            }

            if (value.Contains("White"))
            {
                value = value.White();
            }

            if (value.Contains("Black"))
            {
                value = value.Black();
            }

            return value;
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
                shellState.ConsoleManager.Error.WriteLine("Error saving preferences".SetColor(programState.ErrorColor));
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
                    shellState.ConsoleManager.Error.WriteLine((commandInput.Arguments[1].Text + " does not have a configured value").SetColor(programState.ErrorColor));
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

        public override CommandInputSpecification InputSpec { get; } = CommandInputSpecification.Create("pref")
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
