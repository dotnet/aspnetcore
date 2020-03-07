// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher
{
    public class PrefixConsoleReporter : ConsoleReporter
    {
        private object _lock = new object();

        private readonly string _prefix;

        public PrefixConsoleReporter(string prefix, IConsole console, bool verbose, bool quiet)
            : base(console, verbose, quiet)
        {
            _prefix = prefix;
        }

        protected override void WriteLine(TextWriter writer, string message, ConsoleColor? color)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                writer.Write(_prefix);
                Console.ResetColor();

                base.WriteLine(writer, message, color);
            }
        }
    }
}
