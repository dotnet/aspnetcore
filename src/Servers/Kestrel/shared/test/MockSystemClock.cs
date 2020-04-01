// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Testing
{
    public class MockSystemClock : ISystemClock
    {
        private static Random _random = new Random();

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
            return (long)(_random.NextDouble() * (maxValue - minValue) + minValue);
        }
    }
}
