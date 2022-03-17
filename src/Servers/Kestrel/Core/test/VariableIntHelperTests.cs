// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Net.Http;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class VariableIntHelperTests
{
    [Theory]
    [MemberData(nameof(IntegerData))]
    public void CheckDecoding(long expected, byte[] input)
    {
        var decoded = VariableLengthIntegerHelper.GetInteger(new ReadOnlySequence<byte>(input), out _, out _);
        Assert.Equal(expected, decoded);
    }

    [Theory]
    [MemberData(nameof(IntegerData))]
    public void CheckEncoding(long input, byte[] expected)
    {
        var outputBuffer = new Span<byte>(new byte[8]);
        var encodedLength = VariableLengthIntegerHelper.WriteInteger(outputBuffer, input);
        Assert.Equal(expected.Length, encodedLength);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], outputBuffer[i]);
        }
    }

    public static TheoryData<long, byte[]> IntegerData
    {
        get
        {
            var data = new TheoryData<long, byte[]>();

            data.Add(151288809941952652, new byte[] { 0xc2, 0x19, 0x7c, 0x5e, 0xff, 0x14, 0xe8, 0x8c });
            data.Add(494878333, new byte[] { 0x9d, 0x7f, 0x3e, 0x7d });
            data.Add(15293, new byte[] { 0x7b, 0xbd });
            data.Add(37, new byte[] { 0x25 });

            return data;
        }
    }
}
