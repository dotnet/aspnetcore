// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Internal;

internal static class QueryStringDecoder
{
    /// <summary>
    /// Decodes an encoded query string name or value ('+' as space, then percent-decoding) directly into a string.
    /// </summary>
    public static string DecodeToString(ReadOnlyMemory<char> chars)
    {
        var source = chars.Span;
        if (!source.ContainsAny('%', '+'))
        {
            return chars.ToString();
        }

        if (!source.Contains('+'))
        {
            return Uri.UnescapeDataString(source);
        }

        // Uri.UnescapeDataString returns the same instance when there is nothing to unescape.
        var replaced = string.Create(source.Length, chars, static (destination, chars) => chars.Span.Replace(destination, '+', ' '));
        return Uri.UnescapeDataString(replaced);
    }
}
