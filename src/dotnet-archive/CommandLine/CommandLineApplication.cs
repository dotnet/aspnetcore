// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.CommandLine
{
    internal class CommandLineApplication
    {
        private enum ParseOptionResult
        {
            Succeeded,
            ShowHelp,
            ShowVersion,
            UnexpectedArgs,
        }

        // Indicates whether the parser should throw an exception when it runs into an unexpected argument.
        // If this field is set to false, the parser will stop parsing when it sees an unexpected argument, and all
        // remaining arguments, including the first unexpected argument, will be stored in RemainingArguments property.
        private readonly bool _throwOnUnexpectedArg;

        public CommandLineApplication(bool throwOnUnexpectedArg = true)
        {
            _throwOnUnexpectedArg = throwOnUnexpectedArg;
            Options = new List<CommandOption>();
            Arguments = new List<CommandArgument>();
            Commands = new List<CommandLineApplication>();
            RemainingArguments = new List<string>();
            Invoke = () => 0;
        }

        public CommandLineApplication Parent { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Syntax { get; set; }
        public string Description { get; set; }
        public List<CommandOption> Options { get; private set; }
        public CommandOption OptionHelp { get; private set; }
        public CommandOption OptionVersion { get; private set; }
        public List<CommandArgument> Arguments { get; private set; }
        public List<string> RemainingArguments { get; private set; }
        public bool IsShowingInformation { get; protected set; }  // Is showing help or version?
        public Func<int> Invoke { get; set; }
        public Func<string> LongVersionGetter { get; set; }
        public Func<string> ShortVersionGetter { get; set; }
        public List<CommandLineApplication> Commands { get; private set; }
        public bool HandleResponseFiles { get; set; }
        public bool AllowArgumentSeparator { get; set; }
        public bool HandleRemainingArguments { get; set; }
        public string ArgumentSeparatorHelpText { get; set; }

        public CommandLineApplication AddCommand(string name, bool throwOnUnexpectedArg = true)
        {
            return AddCommand(name, _ => { }, throwOnUnexpectedArg);
        }

        public CommandLineApplication AddCommand(string name, Action<CommandLineApplication> configuration,
            bool throwOnUnexpectedArg = true)
        {
            var command = new CommandLineApplication(throwOnUnexpectedArg) { Name = name };
            return AddCommand(command, configuration, throwOnUnexpectedArg);
        }

        public CommandLineApplication AddCommand(CommandLineApplication command, bool throwOnUnexpectedArg = true)
        {
            return AddCommand(command, _ => { }, throwOnUnexpectedArg);
        }

        public CommandLineApplication AddCommand(
            CommandLineApplication command,
            Action<CommandLineApplication> configuration,
            bool throwOnUnexpectedArg = true)
        {
            if (command == null || configuration == null)
            {
                throw new NullReferenceException();
            }

            command.Parent = this;
            Commands.Add(command);
            configuration(command);
            return command;
        }

        public CommandOption Option(string template, string description, CommandOptionType optionType)
        {
            return Option(template, description, optionType, _ => { });
        }

        public CommandOption Option(string template, string description, CommandOptionType optionType, Action<CommandOption> configuration)
        {
            var option = new CommandOption(template, optionType) { Description = description };
            Options.Add(option);
            configuration(option);
            return option;
        }

        public CommandArgument Argument(string name, string description, bool multipleValues = false)
        {
            return Argument(name, description, _ => { }, multipleValues);
        }

        public CommandArgument Argument(string name, string description, Action<CommandArgument> configuration, bool multipleValues = false)
        {
            var lastArg = Arguments.LastOrDefault();
            if (lastArg != null && lastArg.MultipleValues)
            {
                var message = string.Format(LocalizableStrings.LastArgumentMultiValueError,
                    lastArg.Name);
                throw new InvalidOperationException(message);
            }

            var argument = new CommandArgument { Name = name, Description = description, MultipleValues = multipleValues };
            Arguments.Add(argument);
            configuration(argument);
            return argument;
        }

        public void OnExecute(Func<int> invoke)
        {
            Invoke = invoke;
        }

        public void OnExecute(Func<Task<int>> invoke)
        {
            Invoke = () => invoke().Result;
        }

        public int Execute(params string[] args)
        {
            CommandLineApplication command = this;
            CommandArgumentEnumerator arguments = null;

            if (HandleResponseFiles)
            {
                args = ExpandResponseFiles(args).ToArray();
            }

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                bool isLongOption = arg.StartsWith("--");
                if (arg == "-?" || arg == "/?")
                {
                    command.ShowHelp();
                    return 0;
                }
                else if (isLongOption || arg.StartsWith("-"))
                {
                    CommandOption option;

                    var result = ParseOption(isLongOption, command, args, ref index, out option);
                  

                    if (result == ParseOptionResult.ShowHelp)
                    {
                        command.ShowHelp();
                        return 0;
                    }
                    else if (result == ParseOptionResult.ShowVersion)
                    {
                        command.ShowVersion();
                        return 0;
                    }
                    else if (result == ParseOptionResult.UnexpectedArgs)
                    {
                        break;
                    }
                }
                else
                {
                    var subcommand = ParseSubCommand(arg, command);
                    if (subcommand != null)
                    {
                        command = subcommand;
                    }
                    else
                    {
                        if (arguments == null || arguments.CommandName != command.Name)
                        {
                            arguments = new CommandArgumentEnumerator(command.Arguments.GetEnumerator(), command.Name);
                        }

                        if (arguments.MoveNext())
                        {
                            arguments.Current.Values.Add(arg);
                        }
                        else
                        {
                            HandleUnexpectedArg(command, args, index, argTypeName: "command or argument");
                            break;
                        }
                    }
                }
            }

            if (Commands.Count > 0 && command == this)
            {
                throw new CommandParsingException(
                    command,
                    "Required command missing",
                    isRequiredSubCommandMissing: true);
            }

            return command.Invoke();
        }

        private ParseOptionResult ParseOption(
            bool isLongOption,
            CommandLineApplication command, 
            string[] args, 
            ref int index,
            out CommandOption option)
        {
            option = null;
            ParseOptionResult result = ParseOptionResult.Succeeded;
            var arg = args[index];

            int optionPrefixLength = isLongOption ? 2 : 1;
            string[] optionComponents = arg.Substring(optionPrefixLength).Split(new[] { ':', '=' }, 2);
            string optionName = optionComponents[0];
            
            if (isLongOption)
            {
                option = command.Options.SingleOrDefault(
                    opt => string.Equals(opt.LongName, optionName, StringComparison.Ordinal));
            }
            else
            {
                option = command.Options.SingleOrDefault(
                    opt => string.Equals(opt.ShortName, optionName, StringComparison.Ordinal));

                if (option == null)
                {
                    option = command.Options.SingleOrDefault(
                        opt => string.Equals(opt.SymbolName, optionName, StringComparison.Ordinal));
                }
            }

            if (option == null)
            {
                if (isLongOption && string.IsNullOrEmpty(optionName) &&
                    !command._throwOnUnexpectedArg && AllowArgumentSeparator)
                {
                    // a stand-alone "--" is the argument separator, so skip it and
                    // handle the rest of the args as unexpected args
                    index++;
                }

                HandleUnexpectedArg(command, args, index, argTypeName: "option");
                result = ParseOptionResult.UnexpectedArgs;
            }
            else if (command.OptionHelp == option)
            {
                result = ParseOptionResult.ShowHelp;
            }
            else if (command.OptionVersion == option)
            {
                result = ParseOptionResult.ShowVersion;
            }
            else
            {
                if (optionComponents.Length == 2)
                {
                    if (!option.TryParse(optionComponents[1]))
                    {
                        command.ShowHint();
                        throw new CommandParsingException(command,
                            String.Format(LocalizableStrings.UnexpectedValueForOptionError, optionComponents[1], optionName));
                    }
                }
                else
                {
                    if (option.OptionType == CommandOptionType.NoValue ||
                        option.OptionType == CommandOptionType.BoolValue)
                    {
                        // No value is needed for this option
                        option.TryParse(null);
                    }
                    else
                    {
                        index++;

                        if (index < args.Length)
                        {
                            arg = args[index];
                            if (!option.TryParse(arg))
                            {
                                command.ShowHint();
                                throw new CommandParsingException(
                                    command,
                                    String.Format(LocalizableStrings.UnexpectedValueForOptionError, arg, optionName));
                            }
                        }
                        else
                        {
                            command.ShowHint();
                            throw new CommandParsingException(
                                command,
                                String.Format(LocalizableStrings.OptionRequiresSingleValueWhichIsMissing, arg, optionName));
                        }
                    }
                }
            }

            return result;
        }

        private CommandLineApplication ParseSubCommand(string arg, CommandLineApplication command)
        {
            foreach (var subcommand in command.Commands)
            {
                if (string.Equals(subcommand.Name, arg, StringComparison.OrdinalIgnoreCase))
                {
                    return subcommand;
                }
            }

            return null;
        }

        // Helper method that adds a help option
        public CommandOption HelpOption(string template)
        {
            // Help option is special because we stop parsing once we see it
            // So we store it separately for further use
            OptionHelp = Option(template, LocalizableStrings.ShowHelpInfo, CommandOptionType.NoValue);

            return OptionHelp;
        }

        public CommandOption VersionOption(string template,
                                           string shortFormVersion,
                                           string longFormVersion = null)
        {
            if (longFormVersion == null)
            {
                return VersionOption(template, () => shortFormVersion);
            }
            else
            {
                return VersionOption(template, () => shortFormVersion, () => longFormVersion);
            }
        }

        // Helper method that adds a version option
        public CommandOption VersionOption(string template,
                                           Func<string> shortFormVersionGetter,
                                           Func<string> longFormVersionGetter = null)
        {
            // Version option is special because we stop parsing once we see it
            // So we store it separately for further use
            OptionVersion = Option(template, LocalizableStrings.ShowVersionInfo, CommandOptionType.NoValue);
            ShortVersionGetter = shortFormVersionGetter;
            LongVersionGetter = longFormVersionGetter ?? shortFormVersionGetter;

            return OptionVersion;
        }

        // Show short hint that reminds users to use help option
        public void ShowHint()
        {
            if (OptionHelp != null)
            {
                Console.WriteLine(string.Format(LocalizableStrings.ShowHintInfo, OptionHelp.LongName));
            }
        }

        // Show full help
        public void ShowHelp(string commandName = null)
        {
            var headerBuilder = new StringBuilder(LocalizableStrings.UsageHeader);
            var usagePrefixLength = headerBuilder.Length;
            for (var cmd = this; cmd != null; cmd = cmd.Parent)
            {
                cmd.IsShowingInformation = true;
                if (cmd != this && cmd.Arguments.Any())
                {
                    var args = string.Join(" ", cmd.Arguments.Select(arg => arg.Name));
                    headerBuilder.Insert(usagePrefixLength, string.Format(" {0} {1}", cmd.Name, args));
                }
                else
                {
                    headerBuilder.Insert(usagePrefixLength, string.Format(" {0}", cmd.Name));
                }
            }

            CommandLineApplication target;

            if (commandName == null || string.Equals(Name, commandName, StringComparison.OrdinalIgnoreCase))
            {
                target = this;
            }
            else
            {
                target = Commands.SingleOrDefault(cmd => string.Equals(cmd.Name, commandName, StringComparison.OrdinalIgnoreCase));

                if (target != null)
                {
                    headerBuilder.AppendFormat(" {0}", commandName);
                }
                else
                {
                    // The command name is invalid so don't try to show help for something that doesn't exist
                    target = this;
                }

            }

            var optionsBuilder = new StringBuilder();
            var commandsBuilder = new StringBuilder();
            var argumentsBuilder = new StringBuilder();
            var argumentSeparatorBuilder = new StringBuilder();

            int maxArgLen = 0;
            for (var cmd = target; cmd != null; cmd = cmd.Parent)
            {
                if (cmd.Arguments.Any())
                {
                    if (cmd == target)
                    {
                        headerBuilder.Append(LocalizableStrings.UsageArgumentsToken);
                    }

                    if (argumentsBuilder.Length == 0)
                    {
                        argumentsBuilder.AppendLine();
                        argumentsBuilder.AppendLine(LocalizableStrings.UsageArgumentsHeader);
                    }

                    maxArgLen = Math.Max(maxArgLen, MaxArgumentLength(cmd.Arguments));
                }
            }

            for (var cmd = target; cmd != null; cmd = cmd.Parent)
            {
                if (cmd.Arguments.Any())
                {
                    foreach (var arg in cmd.Arguments)
                    {
                        argumentsBuilder.AppendFormat(
                            "  {0}{1}", 
                            arg.Name.PadRight(maxArgLen + 2), 
                            arg.Description);
                        argumentsBuilder.AppendLine();
                    }
                }
            }

            if (target.Options.Any())
            {
                headerBuilder.Append(LocalizableStrings.UsageOptionsToken);

                optionsBuilder.AppendLine();
                optionsBuilder.AppendLine(LocalizableStrings.UsageOptionsHeader);
                var maxOptLen = MaxOptionTemplateLength(target.Options);
                var outputFormat = string.Format("  {{0, -{0}}}{{1}}", maxOptLen + 2);
                foreach (var opt in target.Options)
                {
                    optionsBuilder.AppendFormat(outputFormat, opt.Template, opt.Description);
                    optionsBuilder.AppendLine();
                }
            }

            if (target.Commands.Any())
            {
                headerBuilder.Append(LocalizableStrings.UsageCommandToken);

                commandsBuilder.AppendLine();
                commandsBuilder.AppendLine(LocalizableStrings.UsageCommandsHeader);
                var maxCmdLen = MaxCommandLength(target.Commands);
                var outputFormat = string.Format("  {{0, -{0}}}{{1}}", maxCmdLen + 2);
                foreach (var cmd in target.Commands.OrderBy(c => c.Name))
                {
                    commandsBuilder.AppendFormat(outputFormat, cmd.Name, cmd.Description);
                    commandsBuilder.AppendLine();
                }

                if (OptionHelp != null)
                {
                    commandsBuilder.AppendLine();
                    commandsBuilder.AppendFormat(LocalizableStrings.UsageCommandsDetailHelp, Name);
                    commandsBuilder.AppendLine();
                }
            }

            if (target.AllowArgumentSeparator || target.HandleRemainingArguments)
            {
                if (target.AllowArgumentSeparator)
                {
                    headerBuilder.Append(LocalizableStrings.UsageCommandAdditionalArgs);
                }
                else
                {
                    headerBuilder.Append(LocalizableStrings.UsageCommandArgs);
                }

                if (!string.IsNullOrEmpty(target.ArgumentSeparatorHelpText))
                {
                    argumentSeparatorBuilder.AppendLine();
                    argumentSeparatorBuilder.AppendLine(LocalizableStrings.UsageCommandsAdditionalArgsHeader);
                    argumentSeparatorBuilder.AppendLine(String.Format(" {0}", target.ArgumentSeparatorHelpText));
                    argumentSeparatorBuilder.AppendLine();
                }
            }

            headerBuilder.AppendLine();

            var nameAndVersion = new StringBuilder();
            nameAndVersion.AppendLine(GetFullNameAndVersion());
            nameAndVersion.AppendLine();

            Console.Write("{0}{1}{2}{3}{4}{5}", nameAndVersion, headerBuilder, argumentsBuilder, optionsBuilder, commandsBuilder, argumentSeparatorBuilder);
        }

        public void ShowVersion()
        {
            for (var cmd = this; cmd != null; cmd = cmd.Parent)
            {
                cmd.IsShowingInformation = true;
            }

            Console.WriteLine(FullName);
            Console.WriteLine(LongVersionGetter());
        }

        public string GetFullNameAndVersion()
        {
            return ShortVersionGetter == null ? FullName : string.Format("{0} {1}", FullName, ShortVersionGetter());
        }

        public void ShowRootCommandFullNameAndVersion()
        {
            var rootCmd = this;
            while (rootCmd.Parent != null)
            {
                rootCmd = rootCmd.Parent;
            }

            Console.WriteLine(rootCmd.GetFullNameAndVersion());
            Console.WriteLine();
        }

        private int MaxOptionTemplateLength(IEnumerable<CommandOption> options)
        {
            var maxLen = 0;
            foreach (var opt in options)
            {
                maxLen = opt.Template.Length > maxLen ? opt.Template.Length : maxLen;
            }
            return maxLen;
        }

        private int MaxCommandLength(IEnumerable<CommandLineApplication> commands)
        {
            var maxLen = 0;
            foreach (var cmd in commands)
            {
                maxLen = cmd.Name.Length > maxLen ? cmd.Name.Length : maxLen;
            }
            return maxLen;
        }

        private int MaxArgumentLength(IEnumerable<CommandArgument> arguments)
        {
            var maxLen = 0;
            foreach (var arg in arguments)
            {
                maxLen = arg.Name.Length > maxLen ? arg.Name.Length : maxLen;
            }
            return maxLen;
        }

        private void HandleUnexpectedArg(CommandLineApplication command, string[] args, int index, string argTypeName)
        {
            if (command._throwOnUnexpectedArg)
            {
                command.ShowHint();
                throw new CommandParsingException(command, String.Format(LocalizableStrings.UnexpectedArgumentError, argTypeName, args[index]));
            }
            else
            {
                // All remaining arguments are stored for further use
                command.RemainingArguments.AddRange(new ArraySegment<string>(args, index, args.Length - index));
            }
        }

        private IEnumerable<string> ExpandResponseFiles(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                if (!arg.StartsWith("@", StringComparison.Ordinal))
                {
                    yield return arg;
                }
                else
                {
                    var fileName = arg.Substring(1);

                    var responseFileArguments = ParseResponseFile(fileName);

                    // ParseResponseFile can suppress expanding this response file by
                    // returning null. In that case, we'll treat the response
                    // file token as a regular argument.

                    if (responseFileArguments == null)
                    {
                        yield return arg;
                    }
                    else
                    {
                        foreach (var responseFileArgument in responseFileArguments)
                            yield return responseFileArgument.Trim();
                    }
                }
            }
        }

        private IEnumerable<string> ParseResponseFile(string fileName)
        {
            if (!HandleResponseFiles)
                return null;

            if (!File.Exists(fileName))
            {
                throw new InvalidOperationException(String.Format(LocalizableStrings.ResponseFileNotFoundError, fileName));
            }

            return File.ReadLines(fileName);
        }

        private class CommandArgumentEnumerator : IEnumerator<CommandArgument>
        {
            private readonly IEnumerator<CommandArgument> _enumerator;

            public CommandArgumentEnumerator(
                IEnumerator<CommandArgument> enumerator,
                string commandName)
            {
                CommandName = commandName;
                _enumerator = enumerator;
            }

            public string CommandName { get; }

            public CommandArgument Current
            {
                get
                {
                    return _enumerator.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                if (Current == null || !Current.MultipleValues)
                {
                    return _enumerator.MoveNext();
                }

                // If current argument allows multiple values, we don't move forward and
                // all later values will be added to current CommandArgument.Values
                return true;
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }
    }
}
