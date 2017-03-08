// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Testing
{
    public static class StringExtensions
    {
        public static string EscapeNonPrintable(this string s)
        {
            var ellipsis = s.Length > 128
                ? "..."
                : string.Empty;
            return s.Substring(0, Math.Min(128, s.Length))
                .Replace("\r", @"\x0D")
                .Replace("\n", @"\x0A")
                .Replace("\0", @"\x00")
                + ellipsis;
        }
    }
}