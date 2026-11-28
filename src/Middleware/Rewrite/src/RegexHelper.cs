// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite;

internal static class RegexHelper
{
    // Regex.Match(input, beginning, length) matches exactly like Regex.Match(input.Substring(beginning, length)),
    // and the groups have the same values. The only difference is in Match.Result: "$_" (entire input) and
    // "$`" (text before the match) also see the characters before 'beginning'. When the replacement contains
    // either of these, fall back to matching against the substring.
    public static bool CanMatchInPlace(string replacement)
        => !replacement.Contains("$_", StringComparison.Ordinal) && !replacement.Contains("$`", StringComparison.Ordinal);

    // Matches against path without its leading '/'.
    public static Match MatchAfterLeadingSlash(Regex regex, string path, bool canMatchInPlace)
        => canMatchInPlace ? regex.Match(path, 1, path.Length - 1) : regex.Match(path.Substring(1));
}
