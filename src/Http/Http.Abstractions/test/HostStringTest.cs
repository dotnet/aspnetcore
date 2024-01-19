// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

public class HostStringTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CtorThrows_IfPortIsNotGreaterThanZero(int port)
    {
        // Act and Assert
        ExceptionAssert.ThrowsArgumentOutOfRange(() => new HostString("localhost", port), "port", "The value must be greater than zero.");
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("localhost", "localhost")]
    [InlineData("1.2.3.4", "1.2.3.4")]
    [InlineData("[2001:db8:a0b:12f0::1]", "[2001:db8:a0b:12f0::1]")]
    [InlineData("本地主機", "本地主機")]
    [InlineData("localhost:5000", "localhost")]
    [InlineData("1.2.3.4:5000", "1.2.3.4")]
    [InlineData("[2001:db8:a0b:12f0::1]:5000", "[2001:db8:a0b:12f0::1]")]
    [InlineData("本地主機:5000", "本地主機")]
    public void Domain_ExtractsHostFromValue(string sourceValue, string expectedDomain)
    {
        // Arrange
        var hostString = new HostString(sourceValue);

        // Act
        var result = hostString.Host;

        // Assert
        Assert.Equal(expectedDomain, result);
    }

    [Theory]
    [InlineData("localhost", null)]
    [InlineData("1.2.3.4", null)]
    [InlineData("[2001:db8:a0b:12f0::1]", null)]
    [InlineData("本地主機", null)]
    [InlineData("localhost:5000", 5000)]
    [InlineData("1.2.3.4:5000", 5000)]
    [InlineData("[2001:db8:a0b:12f0::1]:5000", 5000)]
    [InlineData("本地主機:5000", 5000)]
    public void Port_ExtractsPortFromValue(string sourceValue, int? expectedPort)
    {
        // Arrange
        var hostString = new HostString(sourceValue);

        // Act
        var result = hostString.Port;

        // Assert
        Assert.Equal(expectedPort, result);
    }

    [Theory]
    [InlineData("localhost:BLAH")]
    public void Port_ExtractsInvalidPortFromValue(string sourceValue)
    {
        // Arrange
        var hostString = new HostString(sourceValue);

        // Act
        var result = hostString.Port;

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("localhost", 5000, "localhost", 5000)]
    [InlineData("1.2.3.4", 5000, "1.2.3.4", 5000)]
    [InlineData("[2001:db8:a0b:12f0::1]", 5000, "[2001:db8:a0b:12f0::1]", 5000)]
    [InlineData("2001:db8:a0b:12f0::1", 5000, "[2001:db8:a0b:12f0::1]", 5000)]
    [InlineData("本地主機", 5000, "本地主機", 5000)]
    public void Ctor_CreatesFromHostAndPort(string sourceHost, int sourcePort, string expectedHost, int expectedPort)
    {
        // Arrange
        var hostString = new HostString(sourceHost, sourcePort);

        // Act
        var host = hostString.Host;
        var port = hostString.Port;

        // Assert
        Assert.Equal(expectedHost, host);
        Assert.Equal(expectedPort, port);
    }

    [Fact]
    public void Equals_EmptyHostStringAndDefaultHostString()
    {
        // Act and Assert
        Assert.Equal(default(HostString), new HostString(string.Empty));
        Assert.Equal(default(HostString), new HostString(string.Empty));
        // explicitly checking == operator
        Assert.True(new HostString(string.Empty) == default(HostString));
        Assert.True(default(HostString) == new HostString(string.Empty));
    }

    [Fact]
    public void NotEquals_DefaultHostStringAndNonNullHostString()
    {
        // Arrange
        var hostString = new HostString("example.com");

        // Act and Assert
        Assert.NotEqual(default(HostString), hostString);
    }

    [Fact]
    public void NotEquals_EmptyHostStringAndNonNullHostString()
    {
        // Arrange
        var hostString = new HostString("example.com");

        // Act and Assert
        Assert.NotEqual(hostString, new HostString(string.Empty));
    }

    [Theory]
    [InlineData("localHost", "localhost")]
    [InlineData("localHost", "*")] // Any - Used by HttpSys
    [InlineData("localhost:9090", "localHost")]
    [InlineData("example.com:443", "example.com")]
    [InlineData("foo.eXample.com:443", "*.exampLe.com")]
    [InlineData("f.eXample.com:443", "*.exampLe.com")]
    [InlineData("a.b.c.eXample.com:443", "*.exampLe.com")]
    [InlineData("127.0.0.1", "127.0.0.1")]
    [InlineData("127.0.0.1:443", "127.0.0.1")]
    [InlineData("xn--c1yn36f:443", "xn--c1yn36f")]
    [InlineData("點看", "點看")]
    [InlineData("[::ABC]", "[::aBc]")]
    [InlineData("[::1]:80", "[::1]")]
    [InlineData("[::1]:", "[::1]")]
    [InlineData("::1", "[::1]")]
    public void HostMatches(string host, string pattern)
    {
        Assert.True(HostString.MatchesAny(host, new StringSegment[] { pattern }));
    }

    [Theory]
    [InlineData("example.com", "localhost")]
    [InlineData("localhost:9090", "example.com")]
    [InlineData(":80", "localhost")]
    [InlineData(":", "localhost")]
    [InlineData("example.com:443", "*.example.com")]
    [InlineData(".example.com:443", "*.example.com")]
    [InlineData("foo.com:443", "*.example.com")]
    [InlineData("foo.example.com.bar:443", "*.example.com")]
    [InlineData(".com:443", "*.com")]
    [InlineData("xn--c1yn36f:443", "點看")]
    [InlineData("[::1", "[::1]")]
    [InlineData("[::1:80", "[::1]")]
    [InlineData("::1", "::1")] // Brackets are added to the host before the comparison
    public void HostDoesntMatch(string host, string pattern)
    {
        Assert.False(HostString.MatchesAny(host, new StringSegment[] { pattern }));
    }

    [Fact]
    public void HostMatchThrowsForBadPort()
    {
        Assert.Throws<FormatException>(() => HostString.MatchesAny("example.com:1abc", new StringSegment[] { "example.com" }));
    }
}
