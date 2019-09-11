// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal class AnsiTextWriter
    {
        private readonly TextWriter _writer;

        public AnsiTextWriter(TextWriter writer) => _writer = writer;

        public void WriteLine(string text)
        {
            Interpret(text);
            _writer.Write(Environment.NewLine);
        }

        private void Interpret(string value)
        {
            var matches = Regex.Matches(value, "\x1b\\[([0-9]+)?m");

            var start = 0;
            foreach (Match match in matches)
            {
                var length = match.Index - start;
                if (length != 0)
                {
                    _writer.Write(value.Substring(start, length));
                }

                Apply(match.Groups[1].Value);

                start = match.Index + match.Length;
            }

            if (start != value.Length)
            {
                _writer.Write(value.Substring(start));
            }
        }

        private static void Apply(string parameter)
        {
            switch (parameter)
            {
                case "1":
                    ApplyBold();
                    break;

                case "22":
                    ResetBold();
                    break;

                case "30":
                    ApplyColor(ConsoleColor.Black);
                    break;

                case "31":
                    ApplyColor(ConsoleColor.DarkRed);
                    break;

                case "32":
                    ApplyColor(ConsoleColor.DarkGreen);
                    break;

                case "33":
                    ApplyColor(ConsoleColor.DarkYellow);
                    break;

                case "34":
                    ApplyColor(ConsoleColor.DarkBlue);
                    break;

                case "35":
                    ApplyColor(ConsoleColor.DarkMagenta);
                    break;

                case "36":
                    ApplyColor(ConsoleColor.DarkCyan);
                    break;

                case "37":
                    ApplyColor(ConsoleColor.Gray);
                    break;

                case "39":
                    ResetColor();
                    break;

                default:
                    Debug.Fail("Unsupported parameter: " + parameter);
                    break;
            }
        }

        private static void ApplyBold()
            => Console.ForegroundColor = (ConsoleColor)((int)Console.ForegroundColor | 8);

        private static void ResetBold()
            => Console.ForegroundColor = (ConsoleColor)((int)Console.ForegroundColor & 7);

        private static void ApplyColor(ConsoleColor color)
        {
            var wasBold = ((int)Console.ForegroundColor & 8) != 0;

            Console.ForegroundColor = color;

            if (wasBold)
            {
                ApplyBold();
            }
        }

        private static void ResetColor()
        {
            var wasBold = ((int)Console.ForegroundColor & 8) != 0;

            Console.ResetColor();

            if (wasBold)
            {
                ApplyBold();
            }
        }
    }
}
