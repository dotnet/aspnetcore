// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Tools.Internal
{
    public class ReporterBuilder
    {
        private readonly FormatterBuilder _verbose = new FormatterBuilder();
        private readonly FormatterBuilder _output = new FormatterBuilder();
        private readonly FormatterBuilder _warn = new FormatterBuilder();
        private readonly FormatterBuilder _error = new FormatterBuilder();
        private IConsole _console;

        public ReporterBuilder WithConsole(IConsole console)
        {
            _console = console;
            return this;
        }

        public FormatterBuilder Verbose() => _verbose;
        public FormatterBuilder Output() => _output;
        public FormatterBuilder Warn() => _warn;
        public FormatterBuilder Error() => _error;

        public ReporterBuilder Verbose(Action<FormatterBuilder> configure)
        {
            configure(_verbose);
            return this;
        }

        public ReporterBuilder Output(Action<FormatterBuilder> configure)
        {
            configure(_output);
            return this;
        }

        public ReporterBuilder Warn(Action<FormatterBuilder> configure)
        {
            configure(_warn);
            return this;
        }

        public ReporterBuilder Error(Action<FormatterBuilder> configure)
        {
            configure(_error);
            return this;
        }

        public IReporter Build()
        {
            if (_console == null)
            {
                throw new InvalidOperationException($"Cannot build without first calling {nameof(WithConsole)}");
            }

            return new FormattingReporter(_console,
                _verbose.Build(),
                _output.Build(),
                _warn.Build(),
                _error.Build());
        }
    }
}