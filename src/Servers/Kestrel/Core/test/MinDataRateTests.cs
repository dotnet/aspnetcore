// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class MinDataRateTests
{
    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(double.MaxValue)]
    public void BytesPerSecondValid(double value)
    {
        Assert.Equal(value, new MinDataRate(bytesPerSecond: value, gracePeriod: TimeSpan.MaxValue).BytesPerSecond);
    }

    [Theory]
    [InlineData(double.MinValue)]
    [InlineData(-double.Epsilon)]
    [InlineData(0)]
    public void BytesPerSecondInvalid(double value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MinDataRate(bytesPerSecond: value, gracePeriod: TimeSpan.MaxValue));

        Assert.Equal("bytesPerSecond", exception.ParamName);
        Assert.StartsWith(CoreStrings.PositiveNumberOrNullMinDataRateRequired, exception.Message);
    }

    [Theory]
    [MemberData(nameof(GracePeriodValidData))]
    public void GracePeriodValid(TimeSpan value)
    {
        Assert.Equal(value, new MinDataRate(bytesPerSecond: 1, gracePeriod: value).GracePeriod);
    }

    [Theory]
    [MemberData(nameof(GracePeriodInvalidData))]
    public void GracePeriodInvalid(TimeSpan value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MinDataRate(bytesPerSecond: 1, gracePeriod: value));

        Assert.Equal("gracePeriod", exception.ParamName);
        Assert.StartsWith(CoreStrings.FormatMinimumGracePeriodRequired(Heartbeat.Interval.TotalSeconds), exception.Message);
    }

    public static TheoryData<TimeSpan> GracePeriodValidData => new TheoryData<TimeSpan>
        {
            Heartbeat.Interval + TimeSpan.FromTicks(1),
            TimeSpan.MaxValue
        };

    public static TheoryData<TimeSpan> GracePeriodInvalidData => new TheoryData<TimeSpan>
        {
            TimeSpan.MinValue,
            TimeSpan.FromTicks(-1),
            TimeSpan.Zero,
            Heartbeat.Interval
        };
}
