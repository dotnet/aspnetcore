// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using static Microsoft.Extensions.ApiDescription.Tool.AnsiConstants;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal static class Reporter
    {
        private static AnsiTextWriter Error = new AnsiTextWriter(Console.Error);
        private static AnsiTextWriter Out = new AnsiTextWriter(Console.Out);

        public static bool IsVerbose { get; set; }
        public static bool NoColor { get; set; }
        public static bool PrefixOutput { get; set; }

        public static string Colorize(string value, Func<string, string> colorizeFunc)
            => NoColor ? value : colorizeFunc(value);

        public static void WriteError(string message)
            => WriteLine(Prefix("error:   ", Colorize(message, x => Bold + Red + x + Reset)), isError: true);

        public static void WriteWarning(string message)
            => WriteLine(Prefix("warn:    ", Colorize(message, x => Bold + Yellow + x + Reset)));

        public static void WriteInformation(string message)
            => WriteLine(Prefix("info:    ", message));

        public static void WriteData(string message)
            => WriteLine(Prefix("data:    ", Colorize(message, x => Bold + Gray + x + Reset)));

        public static void WriteVerbose(string message)
        {
            if (IsVerbose)
            {
                WriteLine(Prefix("verbose: ", Colorize(message, x => Gray + x + Reset)));
            }
        }

        private static string Prefix(string prefix, string value)
            => PrefixOutput
                ? string.Join(
                    Environment.NewLine,
                    value.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(l => prefix + l))
                : value;

        private static void WriteLine(string value, bool isError = false)
        {
            if (NoColor)
            {
                (isError ? Console.Error : Console.Out).WriteLine(value);
            }
            else
            {
                (isError ? Error : Out).WriteLine(value);
            }
        }
    }
}
