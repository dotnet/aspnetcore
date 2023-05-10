// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Testing;

public class MockTimeProvider : TimeProvider
{
    private readonly long _timestampFrequency =
        // 10_000_000; // Windows
        // 100_000_000; // 8 zeros, in between
        // 1_000_000_000; // Linux
        Stopwatch.Frequency;
    private long _ticks;

    public MockTimeProvider()
    {
        // Use a random DateTimeOffset to ensure tests that incorrectly use the current DateTimeOffset fail always instead of only rarely.
        // Pick a date between the min DateTimeOffset and a year before the max DateTimeOffset so there's room to advance the clock.
        _ticks = NextLong(0,
            (DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch + TimeSpan.FromDays(365 * 10)).ToTicks(_timestampFrequency));
    }

    public MockTimeProvider(DateTimeOffset now)
    {
        _ticks = (now - DateTimeOffset.UnixEpoch).ToTicks(_timestampFrequency);
    }

    public override DateTimeOffset GetUtcNow()
    {
        UtcNowCalled++;
        var seconds = Interlocked.Read(ref _ticks) / (double)_timestampFrequency;
        return DateTimeOffset.UnixEpoch + TimeSpan.FromSeconds(seconds);
    }

    public void SetUtcNow(DateTimeOffset now)
    {
        Interlocked.Exchange(ref _ticks, (now - DateTimeOffset.UnixEpoch).ToTicks(_timestampFrequency));
    }

    public override long GetTimestamp() => Interlocked.Read(ref _ticks);

    public void SetTimestamp(long now)
    {
        Interlocked.Exchange(ref _ticks, now);
    }

    public override long TimestampFrequency => _timestampFrequency;

    public int UtcNowCalled { get; private set; }

    public void Advance(TimeSpan timeSpan)
    {
        Interlocked.Add(ref _ticks, timeSpan.ToTicks(TimestampFrequency));
    }

    private long NextLong(long minValue, long maxValue)
    {
        return (long)(Random.Shared.NextDouble() * (maxValue - minValue) + minValue);
    }

    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        => throw new NotImplementedException();
}
