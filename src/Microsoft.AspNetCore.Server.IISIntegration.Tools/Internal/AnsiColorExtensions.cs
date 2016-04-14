// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Extensions.Cli.Utils
{
    public static class AnsiColorExtensions
    {
        public static string Red(this string text)
        {
            return "\x1B[31m" + text + "\x1B[39m";
        }

        public static string Yellow(this string text)
        {
            return "\x1B[33m" + text + "\x1B[39m";
        }
    }
}