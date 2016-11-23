// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Tools.Internal
{
    public class ColorFormatter : IFormatter
    {
        // resets foreground color only
        private const string ResetColor = "\x1B[39m";

        private static readonly IDictionary<ConsoleColor, int> AnsiColorCodes
            = new Dictionary<ConsoleColor, int>
            {
                {ConsoleColor.Black, 30},
                {ConsoleColor.DarkRed, 31},
                {ConsoleColor.DarkGreen, 32},
                {ConsoleColor.DarkYellow, 33},
                {ConsoleColor.DarkBlue, 34},
                {ConsoleColor.DarkMagenta, 35},
                {ConsoleColor.DarkCyan, 36},
                {ConsoleColor.Gray, 37},
                {ConsoleColor.DarkGray, 90},
                {ConsoleColor.Red, 91},
                {ConsoleColor.Green, 92},
                {ConsoleColor.Yellow, 93},
                {ConsoleColor.Blue, 94},
                {ConsoleColor.Magenta, 95},
                {ConsoleColor.Cyan, 96},
                {ConsoleColor.White, 97},
            };

        private readonly string _prefix;

        public ColorFormatter(ConsoleColor color)
        {
            _prefix = GetAnsiCode(color);
        }

        public string Format(string text)
            => text?.Length > 0
                ? $"{_prefix}{text}{ResetColor}"
                : text;

        private static string GetAnsiCode(ConsoleColor color)
        {
            int code;
            if (!AnsiColorCodes.TryGetValue(color, out code))
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
            }
            return $"\x1B[{code}m";
        }
    }
}