// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class KestrelServerLimitsTests
{
    [Fact]
    public void MaxResponseBufferSizeDefault()
    {
        Assert.Equal(64 * 1024, (new KestrelServerLimits()).MaxResponseBufferSize);
    }

    [Theory]
    [InlineData((long)-1)]
    [InlineData(long.MinValue)]
    public void MaxResponseBufferSizeInvalid(long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            (new KestrelServerLimits()).MaxResponseBufferSize = value;
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData((long)0)]
    [InlineData((long)1)]
    [InlineData(long.MaxValue)]
    public void MaxResponseBufferSizeValid(long? value)
    {
        var o = new KestrelServerLimits();
        o.MaxResponseBufferSize = value;
        Assert.Equal(value, o.MaxResponseBufferSize);
    }

    [Fact]
    public void MaxRequestBufferSizeDefault()
    {
        Assert.Equal(1024 * 1024, (new KestrelServerLimits()).MaxRequestBufferSize);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void MaxRequestBufferSizeInvalid(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            (new KestrelServerLimits()).MaxRequestBufferSize = value;
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void MaxRequestBufferSizeValid(int? value)
    {
        var o = new KestrelServerLimits();
        o.MaxRequestBufferSize = value;
        Assert.Equal(value, o.MaxRequestBufferSize);
    }

    [Fact]
    public void MaxRequestLineSizeDefault()
    {
        Assert.Equal(8 * 1024, (new KestrelServerLimits()).MaxRequestLineSize);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void MaxRequestLineSizeInvalid(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            (new KestrelServerLimits()).MaxRequestLineSize = value;
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void MaxRequestLineSizeValid(int value)
    {
        var o = new KestrelServerLimits();
        o.MaxRequestLineSize = value;
        Assert.Equal(value, o.MaxRequestLineSize);
    }

    [Fact]
    public void MaxRequestHeadersTotalSizeDefault()
    {
        Assert.Equal(32 * 1024, (new KestrelServerLimits()).MaxRequestHeadersTotalSize);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void MaxRequestHeadersTotalSizeInvalid(int value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits().MaxRequestHeadersTotalSize = value);
        Assert.StartsWith(CoreStrings.PositiveNumberRequired, ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void MaxRequestHeadersTotalSizeValid(int value)
    {
        var o = new KestrelServerLimits();
        o.MaxRequestHeadersTotalSize = value;
        Assert.Equal(value, o.MaxRequestHeadersTotalSize);
    }

    [Fact]
    public void MaxRequestHeaderCountDefault()
    {
        Assert.Equal(100, (new KestrelServerLimits()).MaxRequestHeaderCount);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void MaxRequestHeaderCountInvalid(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            (new KestrelServerLimits()).MaxRequestHeaderCount = value;
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void MaxRequestHeaderCountValid(int value)
    {
        var o = new KestrelServerLimits();
        o.MaxRequestHeaderCount = value;
        Assert.Equal(value, o.MaxRequestHeaderCount);
    }

    [Fact]
    public void KeepAliveTimeoutDefault()
    {
        Assert.Equal(TimeSpan.FromSeconds(130), new KestrelServerLimits().KeepAliveTimeout);
    }

    [Theory]
    [MemberData(nameof(TimeoutValidData))]
    public void KeepAliveTimeoutValid(TimeSpan value)
    {
        Assert.Equal(value, new KestrelServerLimits { KeepAliveTimeout = value }.KeepAliveTimeout);
    }

    [Fact]
    public void KeepAliveTimeoutCanBeSetToInfinite()
    {
        Assert.Equal(TimeSpan.MaxValue, new KestrelServerLimits { KeepAliveTimeout = Timeout.InfiniteTimeSpan }.KeepAliveTimeout);
    }

    [Theory]
    [MemberData(nameof(TimeoutInvalidData))]
    public void KeepAliveTimeoutInvalid(TimeSpan value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits { KeepAliveTimeout = value });

        Assert.Equal("value", exception.ParamName);
        Assert.StartsWith(CoreStrings.PositiveTimeSpanRequired, exception.Message);
    }

    [Fact]
    public void RequestHeadersTimeoutDefault()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), new KestrelServerLimits().RequestHeadersTimeout);
    }

    [Theory]
    [MemberData(nameof(TimeoutValidData))]
    public void RequestHeadersTimeoutValid(TimeSpan value)
    {
        Assert.Equal(value, new KestrelServerLimits { RequestHeadersTimeout = value }.RequestHeadersTimeout);
    }

    [Fact]
    public void RequestHeadersTimeoutCanBeSetToInfinite()
    {
        Assert.Equal(TimeSpan.MaxValue, new KestrelServerLimits { RequestHeadersTimeout = Timeout.InfiniteTimeSpan }.RequestHeadersTimeout);
    }

    [Theory]
    [MemberData(nameof(TimeoutInvalidData))]
    public void RequestHeadersTimeoutInvalid(TimeSpan value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits { RequestHeadersTimeout = value });

        Assert.Equal("value", exception.ParamName);
        Assert.StartsWith(CoreStrings.PositiveTimeSpanRequired, exception.Message);
    }

    [Fact]
    public void MaxConnectionsDefault()
    {
        Assert.Null(new KestrelServerLimits().MaxConcurrentConnections);
        Assert.Null(new KestrelServerLimits().MaxConcurrentUpgradedConnections);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void MaxConnectionsValid(long? value)
    {
        var limits = new KestrelServerLimits
        {
            MaxConcurrentConnections = value
        };

        Assert.Equal(value, limits.MaxConcurrentConnections);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void MaxConnectionsInvalid(long value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits().MaxConcurrentConnections = value);
        Assert.StartsWith(CoreStrings.PositiveNumberOrNullRequired, ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void MaxUpgradedConnectionsValid(long? value)
    {
        var limits = new KestrelServerLimits
        {
            MaxConcurrentUpgradedConnections = value
        };

        Assert.Equal(value, limits.MaxConcurrentUpgradedConnections);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1)]
    public void MaxUpgradedConnectionsInvalid(long value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits().MaxConcurrentUpgradedConnections = value);
        Assert.StartsWith(CoreStrings.NonNegativeNumberOrNullRequired, ex.Message);
    }

    [Fact]
    public void MaxRequestBodySizeDefault()
    {
        // ~28.6 MB (https://www.iis.net/configreference/system.webserver/security/requestfiltering/requestlimits#005)
        Assert.Equal(30000000, new KestrelServerLimits().MaxRequestBodySize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void MaxRequestBodySizeValid(long? value)
    {
        var limits = new KestrelServerLimits
        {
            MaxRequestBodySize = value
        };

        Assert.Equal(value, limits.MaxRequestBodySize);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1)]
    public void MaxRequestBodySizeInvalid(long value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits().MaxRequestBodySize = value);
        Assert.StartsWith(CoreStrings.NonNegativeNumberOrNullRequired, ex.Message);
    }

    [Fact]
    public void MinRequestBodyDataRateDefault()
    {
        Assert.NotNull(new KestrelServerLimits().MinRequestBodyDataRate);
        Assert.Equal(240, new KestrelServerLimits().MinRequestBodyDataRate.BytesPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(5), new KestrelServerLimits().MinRequestBodyDataRate.GracePeriod);
    }

    [Fact]
    public void MinResponseBodyDataRateDefault()
    {
        Assert.NotNull(new KestrelServerLimits().MinResponseDataRate);
        Assert.Equal(240, new KestrelServerLimits().MinResponseDataRate.BytesPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(5), new KestrelServerLimits().MinResponseDataRate.GracePeriod);
    }

    [Fact]
    public void Http2MaxFrameSizeDefault()
    {
        Assert.Equal(1 << 14, new KestrelServerLimits().Http2.MaxFrameSize);
    }

    [Theory]
    [InlineData((1 << 14) - 1)]
    [InlineData(1 << 24)]
    [InlineData(-1)]
    public void Http2MaxFrameSizeInvalid(int value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits().Http2.MaxFrameSize = value);
        Assert.Contains("A value between", ex.Message);
    }

    [Fact]
    public void Http2HeaderTableSizeDefault()
    {
        Assert.Equal(4096, new KestrelServerLimits().Http2.HeaderTableSize);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    public void Http2HeaderTableSizeInvalid(int value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits().Http2.HeaderTableSize = value);
        Assert.StartsWith(CoreStrings.GreaterThanOrEqualToZeroRequired, ex.Message);
    }

    [Fact]
    public void Http2MaxRequestHeaderFieldSizeDefault()
    {
        Assert.Equal(16 * 1024, new KestrelServerLimits().Http2.MaxRequestHeaderFieldSize);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void Http2MaxRequestHeaderFieldSizeInvalid(int value)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new KestrelServerLimits().Http2.MaxRequestHeaderFieldSize = value);
        Assert.StartsWith(CoreStrings.GreaterThanZeroRequired, ex.Message);
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
