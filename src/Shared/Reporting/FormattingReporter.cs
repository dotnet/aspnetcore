// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.Tools.Internal
{
    public class FormattingReporter : IReporter
    {
        private readonly object _writelock = new object();
        private readonly IConsole _console;
        private readonly IFormatter _verbose;
        private readonly IFormatter _warn;
        private readonly IFormatter _output;
        private readonly IFormatter _error;

        public FormattingReporter(IConsole console,
            IFormatter verbose,
            IFormatter output,
            IFormatter warn,
            IFormatter error)
        {
            Ensure.NotNull(console, nameof(console));
            Ensure.NotNull(verbose, nameof(verbose));
            Ensure.NotNull(output, nameof(output));
            Ensure.NotNull(warn, nameof(warn));
            Ensure.NotNull(error, nameof(error));

            _console = console;
            _verbose = verbose;
            _output = output;
            _warn = warn;
            _error = error;
        }


        public void Verbose(string message)
            => Write(_console.Out, _verbose.Format(message));

        public void Output(string message)
            => Write(_console.Out, _output.Format(message));

        public void Warn(string message)
            => Write(_console.Out, _warn.Format(message));

        public void Error(string message)
            => Write(_console.Error, _error.Format(message));

        private void Write(TextWriter writer, string message)
        {
            if (message == null)
            {
                return;
            }

            lock (_writelock)
            {
                writer.WriteLine(message);
            }
        }
    }
}