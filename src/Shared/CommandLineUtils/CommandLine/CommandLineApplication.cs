// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CommandLineUtils;

#pragma warning disable CA1852 // Seal internal types
internal class CommandLineApplication
#pragma warning restore CA1852 // Seal internal types
{
    // Indicates whether the parser should throw an exception when it runs into an unexpected argument. If this is
    // set to true (the default), the parser will throw on the first unexpected argument. Otherwise, all unexpected
    // arguments (including the first) are added to RemainingArguments.
    private readonly bool _throwOnUnexpectedArg;

    // Indicates whether the parser should check remaining arguments for command or option matches after
    // encountering an unexpected argument. Ignored if _throwOnUnexpectedArg is true (the default). If
    // _throwOnUnexpectedArg and this are both false, the first unexpected argument and all remaining arguments are
    // added to RemainingArguments. If _throwOnUnexpectedArg is false and this is true, only unexpected arguments
    // are added to RemainingArguments -- allowing a mix of expected and unexpected arguments, commands and
    // options.
    private readonly bool _continueAfterUnexpectedArg;

    private readonly bool _treatUnmatchedOptionsAsArguments;

    public CommandLineApplication(bool throwOnUnexpectedArg = true, bool continueAfterUnexpectedArg = false, bool treatUnmatchedOptionsAsArguments = false)
    {
        _throwOnUnexpectedArg = throwOnUnexpectedArg;
        _continueAfterUnexpectedArg = continueAfterUnexpectedArg;
        _treatUnmatchedOptionsAsArguments = treatUnmatchedOptionsAsArguments;
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
    public bool ShowInHelpText { get; set; } = true;
    public string ExtendedHelpText { get; set; }
    public readonly List<CommandOption> Options;
    public CommandOption OptionHelp { get; private set; }
    public CommandOption OptionVersion { get; private set; }
    public readonly List<CommandArgument> Arguments;
    public readonly List<string> RemainingArguments;
    public bool IsShowingInformation { get; private set; }  // Is showing help or version?
    public Func<int> Invoke { get; set; }
    public Func<string> LongVersionGetter { get; set; }
    public Func<string> ShortVersionGetter { get; set; }
    public readonly List<CommandLineApplication> Commands;
    public bool AllowArgumentSeparator { get; set; }
    public TextWriter Out { get; set; } = Console.Out;
    public TextWriter Error { get; set; } = Console.Error;

    public IEnumerable<CommandOption> GetOptions()
    {
        var expr = Options.AsEnumerable();
        var rootNode = this;
        while (rootNode.Parent != null)
        {
            rootNode = rootNode.Parent;
            expr = expr.Concat(rootNode.Options.Where(o => o.Inherited));
        }

        return expr;
    }

    public CommandLineApplication Command(string name, Action<CommandLineApplication> configuration,
        bool throwOnUnexpectedArg = true)
    {
        var command = new CommandLineApplication(throwOnUnexpectedArg) { Name = name, Parent = this };
        Commands.Add(command);
        configuration(command);
        return command;
    }

    public CommandOption Option(string template, string description, CommandOptionType optionType)
        => Option(template, description, optionType, _ => { }, inherited: false);

    public CommandOption Option(string template, string description, CommandOptionType optionType, bool inherited)
        => Option(template, description, optionType, _ => { }, inherited);

    public CommandOption Option(string template, string description, CommandOptionType optionType, Action<CommandOption> configuration)
        => Option(template, description, optionType, configuration, inherited: false);

    public CommandOption Option(string template, string description, CommandOptionType optionType, Action<CommandOption> configuration, bool inherited)
    {
        var option = new CommandOption(template, optionType)
        {
            Description = description,
            Inherited = inherited
        };
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
            var message = string.Format(
                CultureInfo.CurrentCulture,
                "The last argument '{0}' accepts multiple values. No more argument can be added.",
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
        CommandOption option = null;
        IEnumerator<CommandArgument> arguments = null;
        var argumentsAssigned = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            var processed = false;
            if (!processed && option == null)
            {
                string[] longOption = null;
                string[] shortOption = null;

                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    longOption = arg.Substring(2).Split(new[] { ':', '=' }, 2);
                }
                else if (arg.StartsWith("-", StringComparison.Ordinal))
                {
                    shortOption = arg.Substring(1).Split(new[] { ':', '=' }, 2);
                }

                if (longOption != null)
                {
                    processed = true;
                    var longOptionName = longOption[0];
                    option = command.GetOptions().SingleOrDefault(opt => string.Equals(opt.LongName, longOptionName, StringComparison.Ordinal));

                    if (option == null && _treatUnmatchedOptionsAsArguments)
                    {
                        if (arguments == null)
                        {
                            arguments = new CommandArgumentEnumerator(command.Arguments.GetEnumerator());
                        }
                        if (arguments.MoveNext())
                        {
                            processed = true;
                            arguments.Current.Values.Add(arg);
                            argumentsAssigned = true;
                            continue;
                        }
                        //else
                        //{
                        //    argumentsAssigned = false;
                        //}
                    }

                    if (option == null)
                    {
                        var ignoreContinueAfterUnexpectedArg = false;
                        if (string.IsNullOrEmpty(longOptionName) &&
                            !command._throwOnUnexpectedArg &&
                            AllowArgumentSeparator)
                        {
                            // Skip over the '--' argument separator then consume all remaining arguments. All
                            // remaining arguments are unconditionally stored for further use.
                            index++;
                            ignoreContinueAfterUnexpectedArg = true;
                        }

                        if (HandleUnexpectedArg(
                            command,
                            args,
                            index,
                            argTypeName: "option",
                            ignoreContinueAfterUnexpectedArg))
                        {
                            continue;
                        }

                        break;
                    }

                    // If we find a help/version option, show information and stop parsing
                    if (command.OptionHelp == option)
                    {
                        command.ShowHelp();
                        return 0;
                    }
                    else if (command.OptionVersion == option)
                    {
                        command.ShowVersion();
                        return 0;
                    }

                    if (longOption.Length == 2)
                    {
                        if (!option.TryParse(longOption[1]))
                        {
                            command.ShowHint();
                            throw new CommandParsingException(command, $"Unexpected value '{longOption[1]}' for option '{option.LongName}'");
                        }
                        option = null;
                    }
                    else if (option.OptionType == CommandOptionType.NoValue)
                    {
                        // No value is needed for this option
                        option.TryParse(null);
                        option = null;
                    }
                }

                if (shortOption != null)
                {
                    processed = true;
                    option = command.GetOptions().SingleOrDefault(opt => string.Equals(opt.ShortName, shortOption[0], StringComparison.Ordinal));

                    if (option == null && _treatUnmatchedOptionsAsArguments)
                    {
                        if (arguments == null)
                        {
                            arguments = new CommandArgumentEnumerator(command.Arguments.GetEnumerator());
                        }
                        if (arguments.MoveNext())
                        {
                            processed = true;
                            arguments.Current.Values.Add(arg);
                            argumentsAssigned = true;
                            continue;
                        }
                        //else
                        //{
                        //    argumentsAssigned = false;
                        //}
                    }

                    // If not a short option, try symbol option
                    if (option == null)
                    {
                        option = command.GetOptions().SingleOrDefault(opt => string.Equals(opt.SymbolName, shortOption[0], StringComparison.Ordinal));
                    }

                    if (option == null)
                    {
                        if (HandleUnexpectedArg(command, args, index, argTypeName: "option"))
                        {
                            continue;
                        }

                        break;
                    }

                    // If we find a help/version option, show information and stop parsing
                    if (command.OptionHelp == option)
                    {
                        command.ShowHelp();
                        return 0;
                    }
                    else if (command.OptionVersion == option)
                    {
                        command.ShowVersion();
                        return 0;
                    }

                    if (shortOption.Length == 2)
                    {
                        if (!option.TryParse(shortOption[1]))
                        {
                            command.ShowHint();
                            throw new CommandParsingException(command, $"Unexpected value '{shortOption[1]}' for option '{option.LongName}'");
                        }
                        option = null;
                    }
                    else if (option.OptionType == CommandOptionType.NoValue)
                    {
                        // No value is needed for this option
                        option.TryParse(null);
                        option = null;
                    }
                }
            }

            if (!processed && option != null)
            {
                processed = true;
                if (!option.TryParse(arg))
                {
                    command.ShowHint();
                    throw new CommandParsingException(command, $"Unexpected value '{arg}' for option '{option.LongName}'");
                }
                option = null;
            }

            if (!processed && !argumentsAssigned)
            {
                var currentCommand = command;
                foreach (var subcommand in command.Commands)
                {
                    if (string.Equals(subcommand.Name, arg, StringComparison.OrdinalIgnoreCase))
                    {
                        processed = true;
                        command = subcommand;
                        break;
                    }
                }

                // If we detect a subcommand
                if (command != currentCommand)
                {
                    processed = true;
                }
            }

            if (!processed)
            {
                if (arguments == null)
                {
                    arguments = new CommandArgumentEnumerator(command.Arguments.GetEnumerator());
                }
                if (arguments.MoveNext())
                {
                    processed = true;
                    arguments.Current.Values.Add(arg);
                }
            }

            if (!processed)
            {
                if (HandleUnexpectedArg(command, args, index, argTypeName: "command or argument"))
                {
                    continue;
                }

                break;
            }
        }

        if (option != null)
        {
            command.ShowHint();
            throw new CommandParsingException(command, $"Missing value for option '{option.LongName}'");
        }

        return command.Invoke();
    }

    // Helper method that adds a help option
    public CommandOption HelpOption(string template)
    {
        // Help option is special because we stop parsing once we see it
        // So we store it separately for further use
        OptionHelp = Option(template, "Show help information", CommandOptionType.NoValue);

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
        OptionVersion = Option(template, "Show version information", CommandOptionType.NoValue);
        ShortVersionGetter = shortFormVersionGetter;
        LongVersionGetter = longFormVersionGetter ?? shortFormVersionGetter;

        return OptionVersion;
    }

    // Show short hint that reminds users to use help option
    public void ShowHint()
    {
        if (OptionHelp != null)
        {
            Out.WriteLine(string.Format(CultureInfo.CurrentCulture, "Specify --{0} for a list of available options and commands.", OptionHelp.LongName));
        }
    }

    // Show full help
    public void ShowHelp(string commandName = null)
    {
        for (var cmd = this; cmd != null; cmd = cmd.Parent)
        {
            cmd.IsShowingInformation = true;
        }

        Out.WriteLine(GetHelpText(commandName));
    }

    public string GetHelpText(string commandName = null)
    {
        var headerBuilder = new StringBuilder("Usage:");
        for (var cmd = this; cmd != null; cmd = cmd.Parent)
        {
            headerBuilder.Insert(6, string.Format(CultureInfo.InvariantCulture, " {0}", cmd.Name));
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
                headerBuilder.AppendFormat(CultureInfo.InvariantCulture, " {0}", commandName);
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

        var arguments = target.Arguments.Where(a => a.ShowInHelpText).ToList();
        if (arguments.Any())
        {
            headerBuilder.Append(" [arguments]");

            argumentsBuilder.AppendLine();
            argumentsBuilder.AppendLine("Arguments:");
            var maxArgLen = arguments.Max(a => a.Name.Length);
            var outputFormat = string.Format(CultureInfo.InvariantCulture, "  {{0, -{0}}}{{1}}", maxArgLen + 2);
            foreach (var arg in arguments)
            {
                argumentsBuilder.AppendFormat(CultureInfo.InvariantCulture, outputFormat, arg.Name, arg.Description);
                argumentsBuilder.AppendLine();
            }
        }

        var options = target.GetOptions().Where(o => o.ShowInHelpText).ToList();
        if (options.Any())
        {
            headerBuilder.Append(" [options]");

            optionsBuilder.AppendLine();
            optionsBuilder.AppendLine("Options:");
            var maxOptLen = options.Max(o => o.Template.Length);
            var outputFormat = string.Format(CultureInfo.InvariantCulture, "  {{0, -{0}}}{{1}}", maxOptLen + 2);
            foreach (var opt in options)
            {
                optionsBuilder.AppendFormat(CultureInfo.InvariantCulture, outputFormat, opt.Template, opt.Description);
                optionsBuilder.AppendLine();
            }
        }

        var commands = target.Commands.Where(c => c.ShowInHelpText).ToList();
        if (commands.Any())
        {
            headerBuilder.Append(" [command]");

            commandsBuilder.AppendLine();
            commandsBuilder.AppendLine("Commands:");
            var maxCmdLen = commands.Max(c => c.Name.Length);
            var outputFormat = string.Format(CultureInfo.InvariantCulture, "  {{0, -{0}}}{{1}}", maxCmdLen + 2);
            foreach (var cmd in commands.OrderBy(c => c.Name))
            {
                commandsBuilder.AppendFormat(CultureInfo.InvariantCulture, outputFormat, cmd.Name, cmd.Description);
                commandsBuilder.AppendLine();
            }

            if (OptionHelp != null)
            {
                commandsBuilder.AppendLine();
                commandsBuilder.Append(FormattableString.Invariant($"Use \"{target.Name} [command] --{OptionHelp.LongName}\" for more information about a command."));
                commandsBuilder.AppendLine();
            }
        }

        if (target.AllowArgumentSeparator)
        {
            headerBuilder.Append(" [[--] <arg>...]");
        }

        headerBuilder.AppendLine();

        var nameAndVersion = new StringBuilder();
        nameAndVersion.AppendLine(GetFullNameAndVersion());
        nameAndVersion.AppendLine();

        return nameAndVersion.ToString()
            + headerBuilder.ToString()
            + argumentsBuilder.ToString()
            + optionsBuilder.ToString()
            + commandsBuilder.ToString()
            + target.ExtendedHelpText;
    }

    public void ShowVersion()
    {
        for (var cmd = this; cmd != null; cmd = cmd.Parent)
        {
            cmd.IsShowingInformation = true;
        }

        Out.WriteLine(FullName);
        Out.WriteLine(LongVersionGetter());
    }

    public string GetFullNameAndVersion()
    {
        return ShortVersionGetter == null ? FullName : string.Format(CultureInfo.InvariantCulture, "{0} {1}", FullName, ShortVersionGetter());
    }

    public void ShowRootCommandFullNameAndVersion()
    {
        var rootCmd = this;
        while (rootCmd.Parent != null)
        {
            rootCmd = rootCmd.Parent;
        }

        Out.WriteLine(rootCmd.GetFullNameAndVersion());
        Out.WriteLine();
    }

    private bool HandleUnexpectedArg(
        CommandLineApplication command,
        string[] args,
        int index,
        string argTypeName,
        bool ignoreContinueAfterUnexpectedArg = false)
    {
        if (command._throwOnUnexpectedArg)
        {
            command.ShowHint();
            throw new CommandParsingException(command, $"Unrecognized {argTypeName} '{args[index]}'");
        }
        else if (_continueAfterUnexpectedArg && !ignoreContinueAfterUnexpectedArg)
        {
            // Store argument for further use.
            command.RemainingArguments.Add(args[index]);
            return true;
        }
        else
        {
            // Store all remaining arguments for later use.
            command.RemainingArguments.AddRange(new ArraySegment<string>(args, index, args.Length - index));
            return false;
        }
    }

    private sealed class CommandArgumentEnumerator : IEnumerator<CommandArgument>
    {
        private readonly IEnumerator<CommandArgument> _enumerator;

        public CommandArgumentEnumerator(IEnumerator<CommandArgument> enumerator)
        {
            _enumerator = enumerator;
        }

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
