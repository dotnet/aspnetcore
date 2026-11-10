// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Internal;

public class QueryStringDecoderTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("value", "value")]
    [InlineData("+", " ")]
    [InlineData("a+b", "a b")]
    [InlineData("++a++", "  a  ")]
    [InlineData("%20", " ")]
    [InlineData("a%20b", "a b")]
    [InlineData("%2B", "+")]
    [InlineData("a+%2B+b", "a + b")]
    [InlineData("%C3%BC", "\u00FC")]
    [InlineData("%F0%9F%98%80", "\U0001F600")]
    [InlineData("a+%C3%BC%20b+%F0%9F%98%80c", "a \u00FC b \U0001F600c")]
    [InlineData("%", "%")]
    [InlineData("%2", "%2")]
    [InlineData("%zz+x", "%zz x")]
    [InlineData("%25", "%")]
    [InlineData("%2520", "%20")]
    public void DecodeToStringDecodesPlusAndPercentEncoding(string encoded, string expected)
    {
        Assert.Equal(expected, QueryStringDecoder.DecodeToString(encoded.AsMemory()));
    }

    [Theory]
    [InlineData("value")]
    [InlineData("a+b")]
    [InlineData("a%20b")]
    [InlineData("a+%2B%20b%")]
    [InlineData("%C3%BC+%F0%9F%98%80")]
    public void DecodeToStringMatchesExistingDecode(string encoded)
    {
        var count = 0;
        foreach (var pair in new QueryStringEnumerable("?" + encoded + "=" + encoded))
        {
            Assert.Equal(pair.DecodeName().ToString(), QueryStringDecoder.DecodeToString(pair.EncodedName));
            Assert.Equal(pair.DecodeValue().ToString(), QueryStringDecoder.DecodeToString(pair.EncodedValue));
            count++;
        }

        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(1000)]
    public void DecodeToStringDecodesLongValues(int repeat)
    {
        var encoded = string.Concat(System.Linq.Enumerable.Repeat("a+%2B", repeat));
        var expected = string.Concat(System.Linq.Enumerable.Repeat("a +", repeat));

        Assert.Equal(expected, QueryStringDecoder.DecodeToString(encoded.AsMemory()));
    }

    [Fact]
    public void DecodeToStringDecodesSliceOfLargerString()
    {
        var query = "x=a+b%20c&y=1";

        Assert.Equal("a b c", QueryStringDecoder.DecodeToString(query.AsMemory(2, 7)));
    }
}
