// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests;

public class HttpsConnectionAdapterOptionsTests
{
    [Fact]
    public void HandshakeTimeoutDefault()
    {
        Assert.Equal(TimeSpan.FromSeconds(10), new HttpsConnectionAdapterOptions().HandshakeTimeout);
    }

    [Theory]
    [MemberData(nameof(TimeoutValidData))]
    public void HandshakeTimeoutValid(TimeSpan value)
    {
        Assert.Equal(value, new HttpsConnectionAdapterOptions { HandshakeTimeout = value }.HandshakeTimeout);
    }

    [Fact]
    public void HandshakeTimeoutCanBeSetToInfinite()
    {
        Assert.Equal(TimeSpan.MaxValue, new HttpsConnectionAdapterOptions { HandshakeTimeout = Timeout.InfiniteTimeSpan }.HandshakeTimeout);
    }

    [Theory]
    [MemberData(nameof(TimeoutInvalidData))]
    public void HandshakeTimeoutInvalid(TimeSpan value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new HttpsConnectionAdapterOptions { HandshakeTimeout = value });

        Assert.Equal("value", exception.ParamName);
        Assert.StartsWith(CoreStrings.PositiveTimeSpanRequired, exception.Message);
    }

    public static TheoryData<TimeSpan> TimeoutValidData => new TheoryData<TimeSpan>
        {
            TimeSpan.FromTicks(1),
            TimeSpan.MaxValue,
        };

    public static TheoryData<TimeSpan> TimeoutInvalidData => new TheoryData<TimeSpan>
        {
            TimeSpan.MinValue,
            TimeSpan.FromTicks(-1),
            TimeSpan.Zero
        };
}
