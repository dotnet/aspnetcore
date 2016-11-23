// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Tools.Internal
{
    public class FormatterBuilder
    {
        private readonly List<IFormatter> _formatters = new List<IFormatter>();

        public FormatterBuilder WithColor(ConsoleColor color)
        {
            _formatters.Add(new ColorFormatter(color));
            return this;
        }

        public FormatterBuilder WithPrefix(string prefix)
        {
            _formatters.Add(new PrefixFormatter(prefix));
            return this;
        }

        public FormatterBuilder When(Func<bool> predicate)
        {
            _formatters.Add(new ConditionalFormatter(predicate));
            return this;
        }

        public IFormatter Build()
        {
            if (_formatters.Count == 0)
            {
                return DefaultFormatter.Instance;
            }

            if (_formatters.Count == 1)
            {
                return _formatters[0];
            }

            return new CompositeFormatter(_formatters.ToArray());
        }
    }
}