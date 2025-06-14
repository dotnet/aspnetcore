// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Shared;

internal static class ReadOnlySpanExtensions
{
    public static bool BinaryReadBoolean(this ReadOnlySpan<byte> span, ref int offset)
    {
        return span[offset++] != 0;
    }

    public static string BinaryReadString(this ReadOnlySpan<byte> span, out int totalBytesRead)
    {
        int length = span.Read7BitEncodedInt(out int prefixLength);
        int stringEnd = prefixLength + length;

        if (span.Length < stringEnd)
        {
            throw new InvalidOperationException("Not enough data to read the string.");
        }

        var strBytes = span.Slice(prefixLength, length);
        totalBytesRead = stringEnd;

        return Encoding.UTF8.GetString(strBytes);
    }
}
