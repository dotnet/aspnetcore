// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Repl.Parsing;

namespace Microsoft.Repl.Commanding
{
    public class DefaultCommandInput<TParseResult>
        where TParseResult : ICoreParseResult
    {
        public DefaultCommandInput(IReadOnlyList<InputElement> commandName, IReadOnlyList<InputElement> arguments, IReadOnlyDictionary<string, IReadOnlyList<InputElement>> options, InputElement selectedElement)
        {
            CommandName = commandName;
            Arguments = arguments;
            Options = options;
            SelectedElement = selectedElement;
        }

        public static bool TryProcess(CommandInputSpecification spec, TParseResult parseResult, out DefaultCommandInput<TParseResult> result, out IReadOnlyList<CommandInputProcessingIssue> processingIssues)
        {
            List<CommandInputProcessingIssue> issues = null;
            List<InputElement> commandNameElements = null;

            foreach (IReadOnlyList<string> commandName in spec.CommandName)
            {
                if (TryProcessCommandName(commandName, parseResult, out List<InputElement> nameElements, out issues))
                {
                    commandNameElements = nameElements;
                    break;
                }
            }

            if (commandNameElements is null)
            {
                result = null;
                processingIssues = issues;
                return false;
            }

            List<InputElement> arguments = new List<InputElement>();
            Dictionary<InputElement, InputElement> options = new Dictionary<InputElement, InputElement>();
            InputElement currentOption = null;
            CommandOptionSpecification currentOptionSpec = null;
            InputElement selectedElement = null;

            for (int i = spec.CommandName.Count; i < parseResult.Sections.Count; ++i)
            {
                //If we're not looking at an option name
                if (!parseResult.Sections[i].StartsWith(spec.OptionPreamble) || parseResult.IsQuotedSection(i))
                {
                    if (currentOption is null)
                    {
                        InputElement currentElement = new InputElement(CommandInputLocation.Argument, parseResult.Sections[i], parseResult.Sections[i], i);

                        if (i == parseResult.SelectedSection)
                        {
                            selectedElement = currentElement;
                        }

                        arguments.Add(currentElement);
                    }
                    else
                    {
                        //If the option isn't a defined one or it is and indicates that it accepts a value, add the section as an option value,
                        //  otherwise add it as an argument
                        if (currentOptionSpec?.AcceptsValue ?? true)
                        {
                            InputElement currentElement = new InputElement(currentOption, CommandInputLocation.OptionValue, parseResult.Sections[i], parseResult.Sections[i], i);

                            if (i == parseResult.SelectedSection)
                            {
                                selectedElement = currentElement;
                            }

                            options[currentOption] = currentElement;
                            currentOption = null;
                            currentOptionSpec = null;
                        }
                        else
                        {
                            InputElement currentElement = new InputElement(CommandInputLocation.Argument, parseResult.Sections[i], parseResult.Sections[i], i);

                            if (i == parseResult.SelectedSection)
                            {
                                selectedElement = currentElement;
                            }

                            arguments.Add(currentElement);
                        }
                    }
                }
                //If we are looking at an option name
                else
                {
                    //Otherwise, check to see whether the previous option had a required argument before committing it
                    if (!(currentOption is null))
                    {
                        options[currentOption] = null;

                        if (currentOptionSpec?.RequiresValue ?? false)
                        {
                            issues.Add(new CommandInputProcessingIssue(CommandInputProcessingIssueKind.MissingRequiredOptionInput, currentOption.Text));
                        }
                    }

                    CommandOptionSpecification optionSpec = spec.Options.FirstOrDefault(x => x.Forms.Any(y => string.Equals(y, parseResult.Sections[i], StringComparison.Ordinal)));

                    if (optionSpec is null)
                    {
                        issues.Add(new CommandInputProcessingIssue(CommandInputProcessingIssueKind.UnknownOption, parseResult.Sections[i]));
                    }

                    currentOption = new InputElement(CommandInputLocation.OptionName, parseResult.Sections[i], optionSpec?.Id, i);

                    if (i == parseResult.SelectedSection)
                    {
                        selectedElement = currentOption;
                    }

                    currentOptionSpec = optionSpec;
                }
            }

            //Clear any option in progress
            if (!(currentOption is null))
            {
                options[currentOption] = null;

                if (currentOptionSpec?.RequiresValue ?? false)
                {
                    issues.Add(new CommandInputProcessingIssue(CommandInputProcessingIssueKind.MissingRequiredOptionInput, currentOption.Text));
                }
            }

            //Check to make sure our argument count is in range, if not add an issue
            if (arguments.Count > spec.MaximumArguments || arguments.Count < spec.MinimumArguments)
            {
                issues.Add(new CommandInputProcessingIssue(CommandInputProcessingIssueKind.ArgumentCountOutOfRange, arguments.Count.ToString()));
            }

            //Build up the dictionary of options by normal form, then validate counts for every option in the spec
            Dictionary<string, IReadOnlyList<InputElement>> optionsByNormalForm = new Dictionary<string, IReadOnlyList<InputElement>>(StringComparer.Ordinal);

            foreach (KeyValuePair<InputElement, InputElement> entry in options)
            {
                if (entry.Key.NormalizedText is null)
                {
                    continue;
                }

                if (!optionsByNormalForm.TryGetValue(entry.Key.NormalizedText, out IReadOnlyList<InputElement> rawBucket))
                {
                    optionsByNormalForm[entry.Key.NormalizedText] = rawBucket = new List<InputElement>();
                }

                List<InputElement> bucket = (List<InputElement>) rawBucket;
                bucket.Add(entry.Value);
            }

            foreach (CommandOptionSpecification optionSpec in spec.Options)
            {
                if (!optionsByNormalForm.TryGetValue(optionSpec.Id, out IReadOnlyList<InputElement> values))
                {
                    optionsByNormalForm[optionSpec.Id] = values = new List<InputElement>();
                }

                if (values.Count < optionSpec.MinimumOccurrences || values.Count > optionSpec.MaximumOccurrences)
                {
                    issues.Add(new CommandInputProcessingIssue(CommandInputProcessingIssueKind.OptionUseCountOutOfRange, values.Count.ToString()));
                }
            }

            result = new DefaultCommandInput<TParseResult>(commandNameElements, arguments, optionsByNormalForm, selectedElement);
            processingIssues = issues;
            return issues.Count == 0;
        }

        private static bool TryProcessCommandName(IReadOnlyList<string> commandName, TParseResult parseResult, out List<InputElement> nameElements, out List<CommandInputProcessingIssue> processingIssues)
        {
            List<CommandInputProcessingIssue> issues = new List<CommandInputProcessingIssue>();
            List<InputElement> commandNameElements = new List<InputElement>();

            if (commandName.Count > parseResult.Sections.Count)
            {
                issues.Add(new CommandInputProcessingIssue(CommandInputProcessingIssueKind.CommandMismatch, commandName[parseResult.Sections.Count]));
            }

            for (int i = 0; i < commandName.Count && i < parseResult.Sections.Count; ++i)
            {
                if (!string.Equals(commandName[i], parseResult.Sections[i], StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new CommandInputProcessingIssue(CommandInputProcessingIssueKind.CommandMismatch, parseResult.Sections[i]));
                }

                commandNameElements.Add(new InputElement(CommandInputLocation.CommandName, parseResult.Sections[i], commandName[i], i));
            }

            processingIssues = issues;

            //If we have a command name mismatch, no point in continuing
            if (issues.Count > 0)
            {
                nameElements = null;
                return false;
            }

            nameElements = commandNameElements;
            return true;
        }

        public InputElement SelectedElement { get; }

        public IReadOnlyList<InputElement> CommandName { get; }

        public IReadOnlyList<InputElement> Arguments { get; }

        public IReadOnlyDictionary<string, IReadOnlyList<InputElement>> Options { get; }
    }
}
