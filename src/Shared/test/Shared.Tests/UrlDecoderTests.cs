// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Shared.Tests;

public class UrlDecoderTests
{
    [Theory]
    [MemberData(nameof(PathTestData))]
    public void StringDecodeRequestLine(string input, string expected)
    {
        var destination = new char[input.Length];
        int length = UrlDecoder.DecodeRequestLine(input.AsSpan(), destination.AsSpan());
        Assert.True(destination.AsSpan(0, length).SequenceEqual(expected.AsSpan()));
    }

    [Theory]
    [MemberData(nameof(UriTestData))]
    public void ByteDecodeRequestLine(byte[] input, byte[] expected)
    {
        var destination = new byte[input.Length];
        int length = UrlDecoder.DecodeRequestLine(input.AsSpan(), destination.AsSpan(), false);
        Assert.True(destination.AsSpan(0, length).SequenceEqual(expected.AsSpan()));
    }

    [Theory]
    [MemberData(nameof(PathTestData))]
    public void StringDecodeInPlace(string input, string expected)
    {
        var destination = new char[input.Length];
        input.CopyTo(destination);
        int length = UrlDecoder.DecodeInPlace(destination.AsSpan());
        Assert.True(destination.AsSpan(0, length).SequenceEqual(expected.AsSpan()));
    }

    [Theory]
    [MemberData(nameof(UriTestData))]
    public void ByteDecodeInPlace(byte[] input, byte[] expected)
    {
        var destination = new byte[input.Length];
        input.AsSpan().CopyTo(destination);
        int length = UrlDecoder.DecodeInPlace(destination.AsSpan(), false);
        Assert.True(destination.AsSpan(0, length).SequenceEqual(expected.AsSpan()));
    }

    [Fact]
    public void StringDestinationShorterThanSourceDecodeRequestLineThrows()
    {
        var source = new char[2];
        Assert.Throws<ArgumentException>(() => UrlDecoder.DecodeRequestLine(source.AsSpan(), source.AsSpan(0, 1)));
    }

    [Fact]
    public void ByteDestinationShorterThanSourceDecodeRequestLineThrows()
    {
        var source = new byte[2];
        Assert.Throws<ArgumentException>(() => UrlDecoder.DecodeRequestLine(source.AsSpan(), source.AsSpan(0, 1), false));
    }

    [Fact]
    public void StringDestinationLargerThanSourceDecodeRequestLineReturnsCorrectLength()
    {
        var source = "/a%20b".ToCharArray();
        var length = UrlDecoder.DecodeRequestLine(source.AsSpan(), new char[source.Length + 10]);
        Assert.Equal(4, length);
    }

    [Fact]
    public void ByteDestinationLargerThanSourceDecodeRequestLineReturnsCorrectLength()
    {
        var source = Encoding.UTF8.GetBytes("/a%20b".ToCharArray());
        var length = UrlDecoder.DecodeRequestLine(source.AsSpan(), new byte[source.Length + 10], false);
        Assert.Equal(4, length);
    }

    [Fact]
    public void StringInputNullCharDecodeInPlaceThrows()
    {
        var source = "%00".ToCharArray();
        Assert.Throws<InvalidOperationException>(() => UrlDecoder.DecodeInPlace(source.AsSpan()));
    }

    [Fact]
    public void ByteInputNullCharDecodeInPlaceThrows()
    {
        var source = Encoding.UTF8.GetBytes("%00");
        Assert.Throws<InvalidOperationException>(() => UrlDecoder.DecodeInPlace(source.AsSpan(), false));
    }

    [Theory]
    [InlineData("%$$")]
    [InlineData("%1")]
    [InlineData("%1$")]
    [InlineData("%%1")]
    [InlineData("%%1$")]
    public void StringInputNonHexDecodeInPlaceLeavesUnencoded(string input)
    {
        var source = input.ToCharArray();
        var length = UrlDecoder.DecodeInPlace(source.AsSpan());
        Assert.Equal(input.Length, length);
        Assert.True(source.AsSpan(0, length).SequenceEqual(input.AsSpan()));
    }

    [Theory]
    [InlineData("%$$")]
    [InlineData("%1")]
    [InlineData("%1$")]
    [InlineData("%%1")]
    [InlineData("%%1$")]
    public void ByteInputNonHexDecodeInPlaceLeavesUnencoded(string input)
    {
        var source = Encoding.UTF8.GetBytes(input.ToCharArray());
        var length = UrlDecoder.DecodeInPlace(source.AsSpan(), false);
        Assert.Equal(source.Length, length);
        Assert.True(source.AsSpan(0, length).SequenceEqual(Encoding.UTF8.GetBytes(input).AsSpan()));
    }

    [Theory]
    [InlineData("%2F")]
    public void ByteFormsEncodingDecodeInPlaceDecodesPercent2F(string input)
    {
        var source = Encoding.UTF8.GetBytes(input.ToCharArray());
        var length = UrlDecoder.DecodeInPlace(source.AsSpan(), true);
        Assert.Equal(1, length);
        Assert.True(source.AsSpan(0, length).SequenceEqual(Encoding.UTF8.GetBytes("/").AsSpan()));
    }

    [Theory]
    [InlineData("%FF%FF%FF%FF")] // FF invalid first byte
    [InlineData("%F7%BF%BF%BF")] // beyond 0x10FFFF
    [InlineData("%F7%C0")] // Following byte does not start with 10xx xxxx
    [InlineData("%F0%81")] // Not enough bytes
    [InlineData("%ED%A0%81")] // Invalid range 0xD800-0xDFFF
    public void StringOutOfUtf8RangeDecodeInPlaceLeavesUnencoded(string input)
    {
        var source = input.ToCharArray();
        var length = UrlDecoder.DecodeInPlace(source.AsSpan());
        Assert.Equal(input.Length, length);
        Assert.True(source.AsSpan(0, length).SequenceEqual(input.AsSpan()));
    }

    [Theory]
    [InlineData("%FF%FF%FF%FF")] // FF invalid first byte
    [InlineData("%F7%BF%BF%BF")] // beyond 0x10FFFF
    [InlineData("%F7%C0")] // Following byte does not start with 10xx xxxx
    [InlineData("%F0%81")] // Not enough bytes
    [InlineData("%ED%A0%81")] // Invalid range 0xD800-0xDFFF
    public void ByteOutOfUtf8RangeDecodeInPlaceLeavesUnencoded(string input)
    {
        var source = Encoding.UTF8.GetBytes(input.ToCharArray());
        var length = UrlDecoder.DecodeInPlace(source.AsSpan(), true);
        Assert.Equal(source.Length, length);
        Assert.True(source.AsSpan(0, length).SequenceEqual(Encoding.UTF8.GetBytes(input).AsSpan()));
    }

    public static IEnumerable<object[]> PathTestData
    {
        get
        {
            return new List<object[]>()
                {
                    new[] { "hello", "hello" },
                    new[] { "/", "/" },
                    new[] { "http://localhost:5000/api", "http://localhost:5000/api" },
                    new[] { "/api/abc", "/api/abc" },
                    new[] { "/api/a%2Fb", "/api/a%2Fb" },
                    new[] { "/a%20b", "/a b" },
                    new[] { "/a%24b", "/a$b" },
                    new[] { "/a%C2%A2b", "/a¢b" },
                    new[] { "/a%E0%A4%B9b", "/aहb" },
                    new[] { "/a%E2%82%ACb", "/a€b" },
                    new[] { "/a%ED%95%9Cb", "/a한b" },
                    new[] { "/a%F0%90%8D%88b", "/a𐍈b" },
                    new[] { "/a%25b", "/a%b" },
                    new[] { "/%E4%BD%A0%E5%A5%BD", "/你好" },
                    new[] { "/a%%2Fb", "/a%%2Fb" },
                    new[] { "/a%2Fb+c", "/a%2Fb+c" },
                    new[] { "/%C3%C3%A1", "/%C3á" },
                    new[] { "/a%20%%b", "/a %%b" },
                };
        }
    }

    public static IEnumerable<object[]> UriTestData
    {
        get
        {
            return PathTestData.Select(x =>
            {
                var input = Encoding.UTF8.GetBytes((string)x[0]);
                var expected = Encoding.UTF8.GetBytes((string)x[1]);
                return new[] { input, expected };
            });
        }
    }
}
