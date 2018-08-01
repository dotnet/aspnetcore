// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.HttpRepl.Suggestions;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class HelpCommand : ICommand<HttpState, ICoreParseResult>
    {
        private static readonly string Name = "help";

        public bool? CanHandle(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            return parseResult.Sections.Count > 0 && string.Equals(parseResult.Sections[0], Name)
                ? (bool?)true
                : null;
        }

        public Task ExecuteAsync(IShellState shellState, HttpState programState, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (shellState.CommandDispatcher is ICommandDispatcher<HttpState, ICoreParseResult> dispatcher)
            {
                if (parseResult.Sections.Count == 1)
                {
                    foreach (ICommand<HttpState, ICoreParseResult> command in dispatcher.Commands)
                    {
                        string help = command.GetHelpSummary(shellState, programState);

                        if (!string.IsNullOrEmpty(help))
                        {
                            shellState.ConsoleManager.WriteLine(help);
                        }
                    }
                }
                else
                {
                    bool anyHelp = false;

                    if (parseResult.Slice(1) is ICoreParseResult continuationParseResult)
                    {
                        foreach (ICommand<HttpState, ICoreParseResult> command in dispatcher.Commands)
                        {
                            string help = command.GetHelpDetails(shellState, programState, continuationParseResult);

                            if (!string.IsNullOrEmpty(help))
                            {
                                anyHelp = true;
                                shellState.ConsoleManager.WriteLine(help);
                            }
                        }
                    }

                    if (!anyHelp)
                    {
                        //Maybe the input is an URL
                        if (parseResult.Sections.Count == 2)
                        {
                            IDirectoryStructure structure = programState.Structure.TraverseTo(parseResult.Sections[1]);
                            if (structure.DirectoryNames.Any())
                            {
                                shellState.ConsoleManager.WriteLine("Child directories:");

                                foreach (string name in structure.DirectoryNames)
                                {
                                    shellState.ConsoleManager.WriteLine("  " + name + "/");
                                }

                                anyHelp = true;
                            }

                            if (structure.RequestInfo != null)
                            {
                                if (structure.RequestInfo.Methods.Count > 0)
                                {
                                    if (anyHelp)
                                    {
                                        shellState.ConsoleManager.WriteLine();
                                    }

                                    anyHelp = true;
                                    shellState.ConsoleManager.WriteLine("Available methods:");

                                    foreach (string method in structure.RequestInfo.Methods)
                                    {
                                        shellState.ConsoleManager.WriteLine("  " + method.ToUpperInvariant());
                                        IReadOnlyList<string> accepts = structure.RequestInfo.ContentTypesByMethod[method];
                                        string acceptsString = string.Join(", ", accepts.Where(x => !string.IsNullOrEmpty(x)));
                                        if (!string.IsNullOrEmpty(acceptsString))
                                        {
                                            shellState.ConsoleManager.WriteLine("    Accepts: " + acceptsString);
                                        }
                                    }
                                }
                            }
                        }

                        if (!anyHelp)
                        {
                            shellState.ConsoleManager.WriteLine("Unable to locate any help information for the specified command");
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        public string GetHelpDetails(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count > 0 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase))
            {
                if (parseResult.Sections.Count > 1)
                {
                    return "Gets help about " + parseResult.Slice(1).CommandText;
                }
                else
                {
                    return "Gets help";
                }
            }

            return null;
        }

        public string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return "help - Gets help";
        }

        public IEnumerable<string> Suggest(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.SelectedSection == 0 &&
                (string.IsNullOrEmpty(parseResult.Sections[parseResult.SelectedSection]) || Name.StartsWith(parseResult.Sections[0].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase)))
            {
                return new[] { Name };
            }
            else if (parseResult.Sections.Count > 1 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase))
            {
                if (shellState.CommandDispatcher is ICommandDispatcher<HttpState, ICoreParseResult> dispatcher 
                    && parseResult.Slice(1) is ICoreParseResult continuationParseResult)
                {
                    HashSet<string> suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (ICommand<HttpState, ICoreParseResult> command in dispatcher.Commands)
                    {
                        IEnumerable<string> commandSuggestions = command.Suggest(shellState, programState, continuationParseResult);

                        if (commandSuggestions != null)
                        {
                            suggestions.UnionWith(commandSuggestions);
                        }
                    }

                    if (continuationParseResult.SelectedSection == 0)
                    {
                        string normalizedCompletionText = continuationParseResult.Sections[0].Substring(0, continuationParseResult.CaretPositionWithinSelectedSection);
                        suggestions.UnionWith(ServerPathCompletion.GetCompletions(programState, normalizedCompletionText));
                    }

                    return suggestions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                }
            }

            return null;
        }
    }
}
