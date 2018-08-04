// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Repl.Commanding
{
    public class CommandInputSpecification
    {
        public IReadOnlyList<IReadOnlyList<string>> CommandName { get; }

        public char OptionPreamble { get; }

        public int MinimumArguments { get; }

        public int MaximumArguments { get; }

        public IReadOnlyList<CommandOptionSpecification> Options { get; }

        public CommandInputSpecification(IReadOnlyList<IReadOnlyList<string>> name, char optionPreamble, IReadOnlyList<CommandOptionSpecification> options, int minimumArgs, int maximumArgs)
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
