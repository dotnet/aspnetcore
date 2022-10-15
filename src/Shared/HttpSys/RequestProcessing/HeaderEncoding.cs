// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static class HeaderEncoding
{
    private static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    internal static unsafe string GetString(byte* pBytes, int byteCount, bool useLatin1)
    {
        if (useLatin1)
        {
            return new ReadOnlySpan<byte>(pBytes, byteCount).GetLatin1StringNonNullCharacters();
        }
        else
        {
            return new ReadOnlySpan<byte>(pBytes, byteCount).GetAsciiOrUTF8StringNonNullCharacters(Encoding);
        }
    }

    internal static byte[] GetBytes(string myString)
    {
        return Encoding.GetBytes(myString);
    }

    internal static Span<byte> GetBytes(string myString, IBufferWriter<byte> writer)
    {
        // Compute the maximum amount of bytes needed for the given string.
        // Include an extra byte for the null terminator.
        Span<byte> buffer = writer.GetSpan(Encoding.GetMaxByteCount(myString.Length) + 1);
        int written = Encoding.GetBytes(myString, buffer);

        // Write a null terminator - the GetBytes() API doesn't add one.
        buffer[written++] = 0;

        // Let the writer know how much was used.
        writer.Advance(written);

        // The resulting Span should not include the null terminator in its length.
        return buffer.Slice(0, written - 1);
    }
}
