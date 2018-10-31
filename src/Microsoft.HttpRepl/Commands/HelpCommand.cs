// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.HttpRepl.Preferences;
using Microsoft.HttpRepl.Suggestions;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
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

        public async Task ExecuteAsync(IShellState shellState, HttpState programState, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (shellState.CommandDispatcher is ICommandDispatcher<HttpState, ICoreParseResult> dispatcher)
            {
                if (parseResult.Sections.Count == 1)
                {
                    CoreGetHelp(shellState, dispatcher, programState);
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
                                shellState.ConsoleManager.WriteLine();
                                shellState.ConsoleManager.WriteLine(help);

                                var structuredCommand = command as CommandWithStructuredInputBase<HttpState, ICoreParseResult>;
                                if (structuredCommand != null && structuredCommand.InputSpec.Options.Any())
                                {
                                    shellState.ConsoleManager.WriteLine();
                                    shellState.ConsoleManager.WriteLine("Options:".Bold());
                                    foreach (var option in structuredCommand.InputSpec.Options)
                                    {
                                        var optionText = string.Empty;
                                        foreach (var form in option.Forms)
                                        {
                                            if (!string.IsNullOrEmpty(optionText))
                                            {
                                                optionText += "|";
                                            }
                                            optionText += form;
                                        }
                                        shellState.ConsoleManager.WriteLine($"    {optionText}");
                                    }
                                }

                                break;
                            }
                        }
                    }

                    if (!anyHelp)
                    {
                        //Maybe the input is an URL
                        if (parseResult.Sections.Count == 2)
                        {

                            if (programState.SwaggerEndpoint != null)
                            {
                                string swaggerRequeryBehaviorSetting = programState.GetStringPreference(WellKnownPreference.SwaggerRequeryBehavior, "auto");

                                if (swaggerRequeryBehaviorSetting.StartsWith("auto", StringComparison.OrdinalIgnoreCase))
                                {
                                    await SetSwaggerCommand.CreateDirectoryStructureForSwaggerEndpointAsync(shellState, programState, programState.SwaggerEndpoint, cancellationToken).ConfigureAwait(false);
                                }
                            }

                            //Structure is null because, for example, SwaggerEndpoint exists but is not reachable.
                            if (programState.Structure != null)
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
                        }

                        if (!anyHelp)
                        {
                            shellState.ConsoleManager.WriteLine("Unable to locate any help information for the specified command");
                        }
                    }
                }
            }
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

        public void CoreGetHelp(IShellState shellState, ICommandDispatcher<HttpState, ICoreParseResult> dispatcher, HttpState programState)
        {
            shellState.ConsoleManager.WriteLine();
            shellState.ConsoleManager.WriteLine("HTTP Commands:".Bold().Cyan());
            shellState.ConsoleManager.WriteLine("Use these commands to execute requests against your application.");
            shellState.ConsoleManager.WriteLine();

            const int navCommandColumn = -15;

            shellState.ConsoleManager.WriteLine($"{"GET",navCommandColumn}{"Issues a GET request."}");
            shellState.ConsoleManager.WriteLine($"{"POST",navCommandColumn}{"Issues a POST request."}");
            shellState.ConsoleManager.WriteLine($"{"PUT",navCommandColumn}{"Issues a PUT request."}");
            shellState.ConsoleManager.WriteLine($"{"DELETE",navCommandColumn}{"Issues a DELETE request."}");
            shellState.ConsoleManager.WriteLine($"{"PATCH",navCommandColumn}{"Issues a PATCH request."}");
            shellState.ConsoleManager.WriteLine($"{"HEAD",navCommandColumn}{"Issues a HEAD request."}");
            shellState.ConsoleManager.WriteLine($"{"OPTIONS",navCommandColumn}{"Issues an OPTIONS request."}");
            shellState.ConsoleManager.WriteLine();
            shellState.ConsoleManager.WriteLine($"{"set header",navCommandColumn}{"Sets or clears a header for all requests. e.g. `set header content-type application/json`"}");
            shellState.ConsoleManager.WriteLine();

            shellState.ConsoleManager.WriteLine();
            shellState.ConsoleManager.WriteLine("Navigation Commands:".Bold().Cyan());
            shellState.ConsoleManager.WriteLine("The REPL allows you to navigate your URL space and focus on specific APIS that you are working on.");
            shellState.ConsoleManager.WriteLine();

            shellState.ConsoleManager.WriteLine($"{"set base",navCommandColumn}{"Set the base URI. e.g. `set base http://locahost:5000`"}");
            shellState.ConsoleManager.WriteLine($"{"set swagger",navCommandColumn}{"Set the URI, relative to your base if set, of the Swagger document for this API. e.g. `set swagger /swagger/v1/swagger.json`"}");
            shellState.ConsoleManager.WriteLine($"{"ls",navCommandColumn}{"Show all endpoints for the current path."}");
            shellState.ConsoleManager.WriteLine($"{"cd",navCommandColumn}{"Append the given directory to the currently selected path, or move up a path when using `cd ..`."}");

            shellState.ConsoleManager.WriteLine();
            shellState.ConsoleManager.WriteLine("Shell Commands:".Bold().Cyan());
            shellState.ConsoleManager.WriteLine("Use these commands to interact with the REPL shell.");
            shellState.ConsoleManager.WriteLine();

            shellState.ConsoleManager.WriteLine($"{"clear",navCommandColumn}{"Removes all text from the shell."}");
            shellState.ConsoleManager.WriteLine($"{"echo [on/off]",navCommandColumn}{"Turns request echoing on or off, show the request that was mode when using request commands."}");
            shellState.ConsoleManager.WriteLine($"{"exit",navCommandColumn}{"Exit the shell."}");

            shellState.ConsoleManager.WriteLine();
            shellState.ConsoleManager.WriteLine("REPL Customization Commands:".Bold().Cyan());
            shellState.ConsoleManager.WriteLine("Use these commands to customize the REPL behavior..");
            shellState.ConsoleManager.WriteLine();

            shellState.ConsoleManager.WriteLine($"{"pref [get/set]",navCommandColumn}{"Allows viewing or changing preferences, e.g. 'pref set editor.command.default 'C:\\Program Files\\Microsoft VS Code\\Code.exe'`"}");
            shellState.ConsoleManager.WriteLine($"{"run",navCommandColumn}{"Runs the script at the given path. A script is a set of commands that can be typed with one command per line."}");
            shellState.ConsoleManager.WriteLine($"{"ui",navCommandColumn}{"Displays the swagger UI page, if available, in the default browser."}");
            shellState.ConsoleManager.WriteLine();
            shellState.ConsoleManager.WriteLine("Use help <COMMAND> to learn more details about individual commands. e.g. `help get`".Bold().Cyan());
            shellState.ConsoleManager.WriteLine();
        }
    }
}
