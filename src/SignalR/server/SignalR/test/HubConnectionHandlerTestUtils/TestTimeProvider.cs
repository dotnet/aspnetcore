// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Tests;

public class TestTimeProvider : TimeProvider
{
    private long _nowTicks;

    public TestTimeProvider()
    {
        // Use a random DateTimeOffset to ensure tests that incorrectly use the current DateTimeOffset fail always instead of only rarely.
        // Pick a date between the min DateTimeOffset and a day before the max DateTimeOffset so there's room to advance the clock.
        _nowTicks = NextLong(0, long.MaxValue - (long)TimeSpan.FromDays(1).TotalSeconds) * TimestampFrequency;
    }

    public override long GetTimestamp() => _nowTicks;

    public void Advance(TimeSpan offset)
    {
        Interlocked.Add(ref _nowTicks, (long)(offset.TotalSeconds * TimestampFrequency));
    }

    private static long NextLong(long minValue, long maxValue)
    {
        return (long)(Random.Shared.NextDouble() * (maxValue - minValue) + minValue);
    }
}
