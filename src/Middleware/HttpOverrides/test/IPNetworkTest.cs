// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net;

namespace Microsoft.AspNetCore.HttpOverrides;

public class IPNetworkTest
{
    [Theory]
    [InlineData("10.1.1.0", 8, "10.1.1.10")]
    [InlineData("174.0.0.0", 7, "175.1.1.10")]
    [InlineData("10.174.0.0", 15, "10.175.1.10")]
    [InlineData("10.168.0.0", 14, "10.171.1.10")]
    [InlineData("192.168.0.1", 31, "192.168.0.0")]
    [InlineData("192.168.0.1", 31, "192.168.0.1")]
    [InlineData("192.168.0.1", 32, "192.168.0.1")]
    [InlineData("192.168.1.1", 0, "0.0.0.0")]
    [InlineData("192.168.1.1", 0, "255.255.255.255")]
    [InlineData("2001:db8:3c4d::", 127, "2001:db8:3c4d::1")]
    [InlineData("2001:db8:3c4d::1", 128, "2001:db8:3c4d::1")]
    [InlineData("2001:db8:3c4d::1", 0, "::")]
    [InlineData("2001:db8:3c4d::1", 0, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    public void Contains_Positive(string prefixText, int length, string addressText)
    {
        var network = new IPNetwork(IPAddress.Parse(prefixText), length);
        Assert.True(network.Contains(IPAddress.Parse(addressText)));
    }

    [Theory]
    [InlineData("10.1.0.0", 16, "10.2.1.10")]
    [InlineData("174.0.0.0", 7, "173.1.1.10")]
    [InlineData("10.174.0.0", 15, "10.173.1.10")]
    [InlineData("10.168.0.0", 14, "10.172.1.10")]
    [InlineData("192.168.0.1", 31, "192.168.0.2")]
    [InlineData("192.168.0.1", 32, "192.168.0.0")]
    [InlineData("2001:db8:3c4d::", 127, "2001:db8:3c4d::2")]
    public void Contains_Negative(string prefixText, int length, string addressText)
    {
        var network = new IPNetwork(IPAddress.Parse(prefixText), length);
        Assert.False(network.Contains(IPAddress.Parse(addressText)));
    }

    [Theory]
    [InlineData("192.168.1.1", 0)]
    [InlineData("192.168.1.1", 32)]
    [InlineData("2001:db8:3c4d::1", 0)]
    [InlineData("2001:db8:3c4d::1", 128)]
    public void Ctor_WithValidFormat_IsSuccessfullyCreated(string prefixText, int prefixLength)
    {
        // Arrange
        var address = IPAddress.Parse(prefixText);

        // Act
        var network = new IPNetwork(address, prefixLength);

        // Assert
        Assert.Equal(prefixText, network.Prefix.ToString());
        Assert.Equal(prefixLength, network.PrefixLength);
    }

    [Theory]
    [InlineData("192.168.1.1", -1)]
    [InlineData("192.168.1.1", 33)]
    [InlineData("2001:db8:3c4d::1", -1)]
    [InlineData("2001:db8:3c4d::1", 129)]
    public void Ctor_WithPrefixLengthOutOfRange_ThrowsArgumentOutOfRangeException(string prefixText, int prefixLength)
    {
        // Arrange
        var address = IPAddress.Parse(prefixText);

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new IPNetwork(address, prefixLength));

        // Assert
        Assert.StartsWith("The prefix length was out of range.", ex.Message);
    }

    [Theory]
    [MemberData(nameof(ValidPrefixWithPrefixLengthData))]
    public void Parse_WithValidFormat_ParsedCorrectly(string input, string expectedPrefix, int expectedPrefixLength)
    {
        // Act
        var network = IPNetwork.Parse(input);

        // Assert
        Assert.Equal(expectedPrefix, network.Prefix.ToString());
        Assert.Equal(expectedPrefixLength, network.PrefixLength);
    }

    [Theory]
    [InlineData(null)]
    [MemberData(nameof(InvalidPrefixOrPrefixLengthData))]
    public void Parse_WithInvalidFormat_ThrowsFormatException(string input)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<FormatException>(() => IPNetwork.Parse(input));
        Assert.Equal("An invalid IP address or prefix length was specified.", ex.Message);
    }

    [Theory]
    [MemberData(nameof(PrefixLengthOutOfRangeData))]
    public void Parse_WithOutOfRangePrefixLength_ThrowsArgumentOutOfRangeException(string input)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => IPNetwork.Parse(input));
        Assert.StartsWith("The prefix length was out of range.", ex.Message);
    }

    [Theory]
    [MemberData(nameof(ValidPrefixWithPrefixLengthData))]
    public void TryParse_WithValidFormat_ParsedCorrectly(string input, string expectedPrefix, int expectedPrefixLength)
    {
        // Act
        var result = IPNetwork.TryParse(input, out var network);

        // Assert
        Assert.True(result);
        Assert.NotNull(network);
        Assert.Equal(expectedPrefix, network.Prefix.ToString());
        Assert.Equal(expectedPrefixLength, network.PrefixLength);
    }

    [Theory]
    [InlineData(null)]
    [MemberData(nameof(InvalidPrefixOrPrefixLengthData))]
    [MemberData(nameof(PrefixLengthOutOfRangeData))]
    public void TryParse_WithInvalidFormat_ReturnsFalse(string input)
    {
        // Act
        var result = IPNetwork.TryParse(input, out var network);

        // Assert
        Assert.False(result);
        Assert.Null(network);
    }

    public static TheoryData<string, string, int> ValidPrefixWithPrefixLengthData() => new()
    {
        // IPv4
        { "10.1.0.0/16", "10.1.0.0", 16 },
        { "10.1.1.0/8", "10.1.1.0", 8 },
        { "174.0.0.0/7", "174.0.0.0", 7 },
        { "10.174.0.0/15", "10.174.0.0", 15 },
        { "10.168.0.0/14", "10.168.0.0", 14 },
        { "192.168.0.1/31", "192.168.0.1", 31 },
        { "192.168.0.1/31", "192.168.0.1", 31 },
        { "192.168.0.1/32", "192.168.0.1", 32 },
        { "192.168.1.1/0", "192.168.1.1", 0 },
        { "192.168.1.1/0", "192.168.1.1", 0 },

        // IPv6
        { "2001:db8:3c4d::/127", "2001:db8:3c4d::", 127 },
        { "2001:db8:3c4d::1/128", "2001:db8:3c4d::1", 128 },
        { "2001:db8:3c4d::1/0", "2001:db8:3c4d::1", 0 },
        { "2001:db8:3c4d::1/0", "2001:db8:3c4d::1", 0 }
    };

    public static TheoryData<string> InvalidPrefixOrPrefixLengthData() => new()
    {
        string.Empty,
        "abcdefg",

        // Missing forward slash
        "10.1.0.016",
        "2001:db8:3c4d::1127",

        // Invalid prefix
        "/16",
        "10.1./16",
        "10.1.0./16",
        "10.1.ABC.0/16",
        "200123:db8:3c4d::/127",
        ":db8:3c4d::/127",
        "2001:?:3c4d::1/0",

        // Invalid prefix length
        "10.1.0.0/",
        "10.1.0.0/16-",
        "10.1.0.0/ABC",
        "2001:db8:3c4d::/",
        "2001:db8:3c4d::1/128-",
        "2001:db8:3c4d::1/ABC"
    };

    public static TheoryData<string> PrefixLengthOutOfRangeData() => new()
    {
        // Negative prefix length
        "10.1.0.0/-16",
        "2001:db8:3c4d::/-127",

        // Prefix length out of range (IPv4)
        "10.1.0.0/33",

        // Prefix length out of range (IPv6)
        "2001:db8:3c4d::/129"
    };
}
