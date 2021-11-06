// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Testing;

public class MockSystemClock : ISystemClock
{
    private long _utcNowTicks;

    public MockSystemClock()
    {
        // Use a random DateTimeOffset to ensure tests that incorrectly use the current DateTimeOffset fail always instead of only rarely.
        // Pick a date between the min DateTimeOffset and a day before the max DateTimeOffset so there's room to advance the clock.
        _utcNowTicks = NextLong(DateTimeOffset.MinValue.Ticks, DateTimeOffset.MaxValue.Ticks - TimeSpan.FromDays(1).Ticks);
    }

    public DateTimeOffset UtcNow
    {
        get
        {
            UtcNowCalled++;
            return new DateTimeOffset(Interlocked.Read(ref _utcNowTicks), TimeSpan.Zero);
        }
        set
        {
            Interlocked.Exchange(ref _utcNowTicks, value.Ticks);
        }
    }

    public long UtcNowTicks => UtcNow.Ticks;

    public DateTimeOffset UtcNowUnsynchronized => UtcNow;

    public int UtcNowCalled { get; private set; }

    private long NextLong(long minValue, long maxValue)
    {
        return (long)(Random.Shared.NextDouble() * (maxValue - minValue) + minValue);
    }
}
