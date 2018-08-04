// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Repl.Commanding
{
    public class CommandInputSpecificationBuilder
    {
        private readonly List<IReadOnlyList<string>> _name;
        private char _optionPreamble;
        private int _minimumArgs;
        private int _maximumArgs;
        private readonly List<CommandOptionSpecification> _options = new List<CommandOptionSpecification>();

        public CommandInputSpecificationBuilder(IReadOnlyList<string> name)
        {
            _name = new List<IReadOnlyList<string>> { name };
            _optionPreamble = '-';
        }

        public CommandInputSpecificationBuilder WithOptionPreamble(char optionChar)
        {
            _optionPreamble = optionChar;
            return this;
        }

        public CommandInputSpecificationBuilder ExactArgCount(int count)
        {
            _minimumArgs = count;
            _maximumArgs = count;
            return this;
        }

        public CommandInputSpecificationBuilder MinimumArgCount(int count)
        {
            _minimumArgs = count;
            if (_maximumArgs < count)
            {
                _maximumArgs = count;
            }

            return this;
        }

        public CommandInputSpecificationBuilder MaximumArgCount(int count)
        {
            _maximumArgs = count;

            if (_minimumArgs > count)
            {
                _minimumArgs = count;
            }

            return this;
        }

        public CommandInputSpecificationBuilder WithOption(CommandOptionSpecification option)
        {
            _options.Add(option);
            return this;
        }

        public CommandInputSpecification Finish()
        {
            return new CommandInputSpecification(_name, _optionPreamble, _options, _minimumArgs, _maximumArgs);
        }

        public CommandInputSpecificationBuilder AlternateName(string baseName, params string[] additionalNameParts)
        {
            List<string> nameParts = new List<string> { baseName };
            nameParts.AddRange(additionalNameParts);
            _name.Add(nameParts);
            return this;
        }
    }
}
