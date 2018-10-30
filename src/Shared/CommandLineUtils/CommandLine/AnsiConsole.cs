// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.CommandLineUtils
{
    internal class AnsiConsole
    {
        private AnsiConsole(TextWriter writer, bool useConsoleColor)
        {
            Writer = writer;

            _useConsoleColor = useConsoleColor;
            if (_useConsoleColor)
            {
                OriginalForegroundColor = Console.ForegroundColor;
            }
        }

        private int _boldRecursion;
        private bool _useConsoleColor;

        public static AnsiConsole GetOutput(bool useConsoleColor)
        {
            return new AnsiConsole(Console.Out, useConsoleColor);
        }

        public static AnsiConsole GetError(bool useConsoleColor)
        {
            return new AnsiConsole(Console.Error, useConsoleColor);
        }

        public TextWriter Writer { get; }

        public ConsoleColor OriginalForegroundColor { get; }

        private void SetColor(ConsoleColor color)
        {
            Console.ForegroundColor = (ConsoleColor)(((int)Console.ForegroundColor & 0x08) | ((int)color & 0x07));
        }

        private void SetBold(bool bold)
        {
            _boldRecursion += bold ? 1 : -1;
            if (_boldRecursion > 1 || (_boldRecursion == 1 && !bold))
            {
                return;
            }

            Console.ForegroundColor = (ConsoleColor)((int)Console.ForegroundColor ^ 0x08);
        }

        public void WriteLine(string message)
        {
            if (!_useConsoleColor)
            {
                Writer.WriteLine(message);
                return;
            }

            var escapeScan = 0;
            for (; ;)
            {
                var escapeIndex = message.IndexOf("\x1b[", escapeScan);
                if (escapeIndex == -1)
                {
                    var text = message.Substring(escapeScan);
                    Writer.Write(text);
                    break;
                }
                else
                {
                    var startIndex = escapeIndex + 2;
                    var endIndex = startIndex;
                    while (endIndex != message.Length &&
                        message[endIndex] >= 0x20 &&
                        message[endIndex] <= 0x3f)
                    {
                        endIndex += 1;
                    }

                    var text = message.Substring(escapeScan, escapeIndex - escapeScan);
                    Writer.Write(text);
                    if (endIndex == message.Length)
                    {
                        break;
                    }

                    switch (message[endIndex])
                    {
                        case 'm':
                            int value;
                            if (int.TryParse(message.Substring(startIndex, endIndex - startIndex), out value))
                            {
                                switch (value)
                                {
                                    case 1:
                                        SetBold(true);
                                        break;
                                    case 22:
                                        SetBold(false);
                                        break;
                                    case 30:
                                        SetColor(ConsoleColor.Black);
                                        break;
                                    case 31:
                                        SetColor(ConsoleColor.Red);
                                        break;
                                    case 32:
                                        SetColor(ConsoleColor.Green);
                                        break;
                                    case 33:
                                        SetColor(ConsoleColor.Yellow);
                                        break;
                                    case 34:
                                        SetColor(ConsoleColor.Blue);
                                        break;
                                    case 35:
                                        SetColor(ConsoleColor.Magenta);
                                        break;
                                    case 36:
                                        SetColor(ConsoleColor.Cyan);
                                        break;
                                    case 37:
                                        SetColor(ConsoleColor.Gray);
                                        break;
                                    case 39:
                                        SetColor(OriginalForegroundColor);
                                        break;
                                }
                            }
                            break;
                    }

                    escapeScan = endIndex + 1;
                }
            }
            Writer.WriteLine();
        }
    }
}
