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
    private long _utcTicks;
    private long _timestamp;

    public MockTimeProvider()
    {
        // Use a random DateTimeOffset to ensure tests that incorrectly use the current DateTimeOffset fail always instead of only rarely.
        var tenYears = TimeSpan.FromDays(365 * 10).Ticks;
        _utcTicks = DateTimeOffset.UtcNow.Ticks + Random.Shared.NextInt64(-tenYears, tenYears);
        // Timestamps often measure system uptime.
        _timestamp = Random.Shared.NextInt64(0, System.GetTimestamp() * 100);
    }

    public MockTimeProvider(DateTimeOffset now)
    {
        _utcTicks = now.Ticks;
        // Timestamps often measure system uptime.
        _timestamp = Random.Shared.NextInt64(0, System.GetTimestamp() * 100);
    }

    public override DateTimeOffset GetUtcNow()
    {
        UtcNowCalled++;
        return new DateTimeOffset(Interlocked.Read(ref _utcTicks), TimeSpan.Zero);
    }

    public override long GetTimestamp() => Interlocked.Read(ref _timestamp);

    public override long TimestampFrequency => _timestampFrequency;

    public int UtcNowCalled { get; private set; }

    public void Advance(TimeSpan timeSpan)
    {
        if (timeSpan < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSpan), timeSpan, "Cannot go back in time.");
        }
        Interlocked.Add(ref _utcTicks, timeSpan.Ticks);
        checked
        {
            Interlocked.Add(ref _timestamp, (long)(timeSpan.Ticks * ((double)_timestampFrequency / TimeSpan.TicksPerSecond)));
        }
    }

    public void AdvanceTo(DateTimeOffset newUtcNow)
    {
        var nowTicks = newUtcNow.UtcTicks;
        var priorTicks = Interlocked.Exchange(ref _utcTicks, nowTicks);
        if (priorTicks > nowTicks)
        {
            var priorTime = new DateTimeOffset(priorTicks, TimeSpan.Zero);
            throw new ArgumentOutOfRangeException(nameof(newUtcNow), newUtcNow, $"Cannot go back in time. The prior time was {priorTime}");
        }
        // Advance Timestamp by the same amount.
        var timestampOffset = (long)((nowTicks - priorTicks) * ((double)_timestampFrequency / TimeSpan.TicksPerSecond));
        Interlocked.Add(ref _timestamp, timestampOffset);
    }

    public void AdvanceTo(long timestamp)
    {
        var priorTimestamp = Interlocked.Exchange(ref _timestamp, timestamp);
        if (priorTimestamp > timestamp)
        {
            throw new ArgumentOutOfRangeException(nameof(timestamp), timestamp, $"Cannot go back in time. The prior timestamp was {priorTimestamp}");
        }
        // Advance UtcNow by the same amount.
        checked
        {
            var utcOffset = (long)((timestamp - priorTimestamp) * ((double)TimeSpan.TicksPerSecond / _timestampFrequency));
            Interlocked.Add(ref _utcTicks, utcOffset);
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        => throw new NotImplementedException();
}
