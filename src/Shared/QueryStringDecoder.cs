// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if NET
using System.Buffers;
using System.Diagnostics;
#endif

namespace Microsoft.AspNetCore.Internal;

internal static class QueryStringDecoder
{
    /// <summary>
    /// Decodes an encoded query string name or value ('+' as space, then percent-decoding).
    /// Returns <paramref name="chars"/> unchanged when there is nothing to decode.
    /// </summary>
    public static ReadOnlyMemory<char> DecodeToMemory(ReadOnlyMemory<char> chars)
    {
        var source = chars.Span;
#if NET
        if (!source.ContainsAny('%', '+'))
        {
            return chars;
        }

        var buffer = new char[source.Length];
        var written = Decode(source, buffer);
        return buffer.AsMemory(0, written);
#else
        if (source.IndexOf('%') < 0 && source.IndexOf('+') < 0)
        {
            return chars;
        }

        return Uri.UnescapeDataString(chars.ToString().Replace('+', ' ')).AsMemory();
#endif
    }

#if NET
    private const int StackAllocThreshold = 256;

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

        char[]? rented = null;
        var buffer = source.Length <= StackAllocThreshold
            ? stackalloc char[StackAllocThreshold]
            : (rented = ArrayPool<char>.Shared.Rent(source.Length));

        var written = Decode(source, buffer);
        var result = new string(buffer[..written]);

        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        return result;
    }

    /// <summary>
    /// Decodes <paramref name="source"/> ('+' as space, then percent-decoding) into <paramref name="destination"/>,
    /// which must be at least as long as <paramref name="source"/>.
    /// </summary>
    /// <returns>The number of characters written to <paramref name="destination"/>.</returns>
    public static int Decode(ReadOnlySpan<char> source, Span<char> destination)
    {
        Debug.Assert(destination.Length >= source.Length);

        source.Replace(destination, '+', ' ');
        var success = Uri.TryUnescapeDataString(destination[..source.Length], destination, out var written);
        Debug.Assert(success);
        return written;
    }
#endif
}
