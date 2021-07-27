// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class MockSystemClock : ISystemClock
    {
        private long _nowTicks;

        public MockSystemClock()
        {
            // Use a random DateTimeOffset to ensure tests that incorrectly use the current DateTimeOffset fail always instead of only rarely.
            // Pick a date between the min DateTimeOffset and a day before the max DateTimeOffset so there's room to advance the clock.
            _nowTicks = NextLong(0, long.MaxValue - TimeSpan.FromDays(1).TotalMilliseconds);
        }

        public long CurrentTicks
        {
            get => _nowTicks;
            set
            {
                Interlocked.Exchange(ref _nowTicks, value);
            }
        }

        private long NextLong(long minValue, long maxValue)
        {
            return (long)(Random.Shared.NextDouble() * (maxValue - minValue) + minValue);
        }
    }
}
