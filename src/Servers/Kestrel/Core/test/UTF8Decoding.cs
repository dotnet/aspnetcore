// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Numerics;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class UTF8DecodingTests
{
    [Theory]
    [InlineData(new byte[] { 0x01 })] // 1 byte: Control character, lowest UTF-8 character we will allow to be decoded since 0x00 is rejected,
    [InlineData(new byte[] { 0xc2, 0xa0 })] // 2 bytes: Non-breaking space, lowest valid UTF-8 that is not a valid ASCII character
    [InlineData(new byte[] { 0xef, 0xbf, 0xbd })] // 3 bytes: Replacement character, highest UTF-8 character currently encoded in the UTF-8 code page
    private void FullUTF8RangeSupported(byte[] encodedBytes)
    {
        var s = HttpUtilities.GetRequestHeaderString(encodedBytes.AsSpan(), HeaderNames.Accept, KestrelServerOptions.DefaultHeaderEncodingSelector, checkForNewlineChars: false);

        Assert.Equal(1, s.Length);
    }

    [Theory]
    [InlineData(new byte[] { 0x00 })] // We reject the null character
    [InlineData(new byte[] { 0x80 })] // First valid Extended ASCII that is not a valid UTF-8 Encoding
    [InlineData(new byte[] { 0x20, 0xac })] // First valid Extended ASCII that is not a valid UTF-8 Encoding
    private void ExceptionThrownForZeroOrNonAscii(byte[] bytes)
    {
        for (var length = bytes.Length; length < Vector<sbyte>.Count * 4 + bytes.Length; length++)
        {
            for (var position = 0; position <= length - bytes.Length; position++)
            {
                var byteRange = Enumerable.Range(1, length).Select(x => (byte)x).ToArray();
                Array.Copy(bytes, 0, byteRange, position, bytes.Length);

                Assert.Throws<InvalidOperationException>(() =>
                    HttpUtilities.GetRequestHeaderString(byteRange.AsSpan(), HeaderNames.Accept, KestrelServerOptions.DefaultHeaderEncodingSelector, checkForNewlineChars: false));
            }
        }
    }
}
