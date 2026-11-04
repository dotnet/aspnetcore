// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Internal.Test;

public class ValueStopwatchTest
{
    [Fact]
    public void IsActiveIsFalseForDefaultValueStopwatch()
    {
        Assert.False(default(ValueStopwatch).IsActive);
    }

    [Fact]
    public void IsActiveIsTrueWhenValueStopwatchStartedWithStartNew()
    {
        Assert.True(ValueStopwatch.StartNew().IsActive);
    }

    [Fact]
    public void GetElapsedTimeThrowsIfValueStopwatchIsDefaultValue()
    {
        var stopwatch = default(ValueStopwatch);
        Assert.Throws<InvalidOperationException>(() => stopwatch.GetElapsedTime());
    }

    [Fact]
    public async Task GetElapsedTimeReturnsTimeElapsedSinceStart()
    {
        var stopwatch = ValueStopwatch.StartNew();
        await Task.Delay(200);
        Assert.True(stopwatch.GetElapsedTime().TotalMilliseconds > 0);
    }
}
