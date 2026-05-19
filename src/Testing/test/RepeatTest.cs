// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

[Repeat]
public class RepeatTest
{
    public static int _runCount = 0;

    [Fact]
    [Repeat(5)]
    public void RepeatLimitIsSetCorrectly()
    {
        Assert.Equal(5, RepeatContext.Current.Limit);
    }

    [Fact]
    [Repeat(5)]
    public void RepeatRunsTestSpecifiedNumberOfTimes()
    {
        Assert.Equal(RepeatContext.Current.CurrentIteration, _runCount);
        _runCount++;
    }

    [Fact]
    public void RepeatCanBeSetOnClass()
    {
        Assert.Equal(10, RepeatContext.Current.Limit);
    }
}

public class LoggedTestXunitRepeatAssemblyTests
{
    [Fact]
    public void RepeatCanBeSetOnAssembly()
    {
        Assert.Equal(1, RepeatContext.Current.Limit);
    }
}
