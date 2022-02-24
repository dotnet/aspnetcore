// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class IsIPV6SegmentTests
{
    [Fact]
    public void IsIPv6_AssertNullRemoteIpAddressReportsCorrectValue()
    {
        // Arrange
        var segement = new IsIPV6Segment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Connection.RemoteIpAddress = null;

        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal("off", results);
    }

    [Fact]
    public void IsIPv6_AssertCorrectBehaviorWhenIPv6IsUsed()
    {
        // Arrange
        var segement = new IsIPV6Segment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334");

        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal("on", results);
    }

    [Fact]
    public void IsIPv6_AssertCorrectBehaviorWhenIPv4IsUsed()
    {
        // Arrange
        var segement = new IsIPV6Segment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("20.30.40.50");

        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal("off", results);
    }
}
