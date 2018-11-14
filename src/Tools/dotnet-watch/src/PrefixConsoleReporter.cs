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

        public PrefixConsoleReporter(IConsole console, bool verbose, bool quiet)
            : base(console, verbose, quiet)
        { }

        protected override void WriteLine(TextWriter writer, string message, ConsoleColor? color)
        {
            const string prefix = "watch : ";

            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                writer.Write(prefix);
                Console.ResetColor();

                base.WriteLine(writer, message, color);
            }
        }
    }
}
