using System.Collections.Generic;

namespace Microsoft.Repl.Commanding
{
    public class CommandInputSpecification
    {
        public IReadOnlyList<string> CommandName { get; }

        public char OptionPreamble { get; }

        public int MinimumArguments { get; }

        public int MaximumArguments { get; }

        public IReadOnlyList<CommandOptionSpecification> Options { get; }

        public CommandInputSpecification(IReadOnlyList<string> name, char optionPreamble, IReadOnlyList<CommandOptionSpecification> options, int minimumArgs, int maximumArgs)
        {
            CommandName = name;
            OptionPreamble = optionPreamble;
            MinimumArguments = minimumArgs;
            MaximumArguments = maximumArgs;

            if (MinimumArguments < 0)
            {
                MinimumArguments = 0;
            }

            if (MaximumArguments < MinimumArguments)
            {
                MaximumArguments = MinimumArguments;
            }

            Options = options;
        }

        public static CommandInputSpecificationBuilder Create(string baseName, params string[] additionalNameParts)
        {
            List<string> nameParts = new List<string> {baseName};
            nameParts.AddRange(additionalNameParts);
            return new CommandInputSpecificationBuilder(nameParts);
        }
    }
}