// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Repl.ConsoleHandling
{
    internal class Writable : IWritable
    {
        private readonly Func<IDisposable> _caretUpdater;
        private readonly Reporter _reporter;

        public Writable(Func<IDisposable> caretUpdater, Reporter reporter)
        {
            _caretUpdater = caretUpdater;
            _reporter = reporter;
        }

        public bool IsCaretVisible
        {
            get => _reporter.IsCaretVisible;
            set => _reporter.IsCaretVisible = value;
        }

        public void Write(char c)
        {
            using (_caretUpdater())
            {
                _reporter.Write(c);
            }
        }

        public void Write(string s)
        {
            using (_caretUpdater())
            {
                _reporter.Write(s);
            }
        }

        public void WriteLine()
        {
            using (_caretUpdater())
            {
                _reporter.WriteLine();
            }
        }

        public void WriteLine(string s)
        {
            using (_caretUpdater())
            {
                _reporter.WriteLine(s);
            }
        }
    }
}
