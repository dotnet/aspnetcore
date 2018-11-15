// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Testing
{
    public class MockSystemClock : ISystemClock
    {
        private long _utcNowTicks = DateTimeOffset.UtcNow.Ticks;

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
    }
}
