// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Repl.ConsoleHandling
{
    public static class AnsiColorExtensions
    {
        public static string Black(this string text)
        {
            return "\x1B[30m" + text + "\x1B[39m";
        }

        public static string Red(this string text)
        {
            return "\x1B[31m" + text + "\x1B[39m";
        }
        public static string Green(this string text)
        {
            return "\x1B[32m" + text + "\x1B[39m";
        }

        public static string Yellow(this string text)
        {
            return "\x1B[33m" + text + "\x1B[39m";
        }

        public static string Blue(this string text)
        {
            return "\x1B[34m" + text + "\x1B[39m";
        }

        public static string Magenta(this string text)
        {
            return "\x1B[35m" + text + "\x1B[39m";
        }

        public static string Cyan(this string text)
        {
            return "\x1B[36m" + text + "\x1B[39m";
        }

        public static string White(this string text)
        {
            return "\x1B[37m" + text + "\x1B[39m";
        }

        public static string Bold(this string text)
        {
            return "\x1B[1m" + text + "\x1B[22m";
        }

        public static string SetColor(this string text, AllowedColors color)
        {
            if (color.HasFlag(AllowedColors.Bold))
            {
                text = text.Bold();
                color = color & ~AllowedColors.Bold;
            }

            switch (color)
            {
                case AllowedColors.Black:
                    return text.Black();
                case AllowedColors.Red:
                    return text.Red();
                case AllowedColors.Green:
                    return text.Green();
                case AllowedColors.Yellow:
                    return text.Yellow();
                case AllowedColors.Blue:
                    return text.Blue();
                case AllowedColors.Magenta:
                    return text.Magenta();
                case AllowedColors.Cyan:
                    return text.Cyan();
                case AllowedColors.White:
                    return text.White();
                default:
                    return text;
            }
        }
    }
}
