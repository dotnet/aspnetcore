// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Testing;

public class MockTimeProvider : TimeProvider
{
    private long _utcNowTicks;

    public MockTimeProvider()
    {
        // Use a random DateTimeOffset to ensure tests that incorrectly use the current DateTimeOffset fail always instead of only rarely.
        // Pick a date between the min DateTimeOffset and a day before the max DateTimeOffset so there's room to advance the clock.
        _utcNowTicks = NextLong(DateTimeOffset.MinValue.Ticks, DateTimeOffset.MaxValue.Ticks - TimeSpan.FromDays(1).Ticks);
    }

    public MockTimeProvider(DateTimeOffset now)
    {
        _utcNowTicks = now.UtcTicks;
    }

    public override DateTimeOffset GetUtcNow()
    {
        UtcNowCalled++;
        return new DateTimeOffset(Interlocked.Read(ref _utcNowTicks), TimeSpan.Zero);
    }

    public void SetUtcNow(DateTimeOffset now)
    {
        Interlocked.Exchange(ref _utcNowTicks, now.Ticks);
    }

    public override long GetTimestamp() => GetUtcNow().Ticks;

    public int UtcNowCalled { get; private set; }

    public void Advance(TimeSpan timeSpan)
    {
        Interlocked.Add(ref _utcNowTicks, timeSpan.Ticks);
    }

    private long NextLong(long minValue, long maxValue)
    {
        return (long)(Random.Shared.NextDouble() * (maxValue - minValue) + minValue);
    }
}
