﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Tests.PatternSegments;

public class LocalAddressSegmentTests
{
    // [Fact]
    public void LocalAddress_AssertSegmentIsCorrect()
    {
        // Arrange
        var segement = new LocalAddressSegment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Connection.LocalIpAddress = IPAddress.Parse("20.30.40.50");
        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Equal("20.30.40.50", results);
    }

    // [Fact]
    public void LocalAddress_AssertNullLocalIpAddressReturnsNull()
    {
        var segement = new LocalAddressSegment();
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        context.HttpContext.Connection.LocalIpAddress = null;
        // Act
        var results = segement.Evaluate(context, null, null);

        // Assert
        Assert.Null(results);
    }
}
